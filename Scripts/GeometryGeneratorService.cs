// FILE: GeometryGeneratorService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains service class for management of geometry generation process from voxel data on chunk columns.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Types of tasks this service supports.
 */
public enum GEOMETRY_TASK_TYPE : byte
{
    GENERATE,
    UPDATE_EDIT
}

/**
 * Represents single geometry generation task for the geometry generator service to process.
 */
public class ChunkColumnGeometryTask(GEOMETRY_TASK_TYPE type, LodLevel lodLevel, int chunkX, uint chunkY, int chunkZ, int priority)
{
    public readonly GEOMETRY_TASK_TYPE type = type;
    public readonly LodLevel lodLevel = lodLevel;
    public readonly int chunkX = chunkX;
    public readonly uint chunkY = chunkY; // only used when updating changed chunks
    public readonly int chunkZ = chunkZ;
    public readonly int priority = priority; // lower value means higher priority
}

/**
 * Represents triplet (column, y, lod) of chunk that needs to have regenerated geometry after voxel editing.
 */
public struct ChunkUpdateTask(ChunkColumnGeometry columnGeometry, uint chunkY, LodLevel lodLevel)
{
    public readonly ChunkColumnGeometry columnGeometry = columnGeometry;
    public readonly uint chunkY = chunkY;
    public readonly LodLevel lodLevel = lodLevel;
}

/**
 * Manages geometry generation for chunk columns and geometry updates of specific chunks due to user editing.
 * Internally uses GeometryGenerator to generate columns and process chunk updates.
 * Processes given tasks in parallel with up to the configured amount of threads.
 * Normal column generation tasks are provided by Root. Requests for chunk updates are provided by VoxelEditService.
 * Unloading of column geometry signaled from Root is processed immediately in batches via the UnloadColumns function.
 * Before a column can have its geometry generated it needs to be fully voxelized together with its 4 neighbors.
 * Columns with finished geometry are signaled further to WorldDisplayService.
 * Lods are generated independently.
 */
public class GeometryGeneratorService : IDisposable
{
    private readonly VoxelStorage voxelStorage;
    private readonly GeometryGenerator geometryGenerator;

    private readonly int chunkCountY;
    private readonly int lodCount;

    // columns with generated or currently generating geometry
    private readonly ConcurrentDictionary<(int, int), ChunkColumnGeometry> activeColumns;
    private readonly PriorityQueue<ChunkColumnGeometryTask, int> taskQueue; // scheduled processable tasks
    private readonly List<ChunkColumnGeometryTask> pendingLockedTasks; // tasks planned, but their columns or their neighbors are not finished
    private readonly Lock taskQueueLock; // locks access to task lists and queues
    private readonly Lock eventLock;

    // signals that given column has generated geometry now
    private Action<ChunkColumnGeometry> columnGeometryGenerated;
    private Action<int, uint, int> chunkGeometryUpdated; // signals that given chunk has now up-to-date geometry
    private Action editingProcessed; // signals that last geometry update request was finished, so that Root can reenable editing
    private readonly List<Thread> workerThreads;
    private readonly ManualResetEventSlim taskAvailable; // signals task availability so that threads can pick them up
    private volatile bool stopWorkers;
    
    public GeometryGeneratorService(VoxelStorage voxelStorage, VoxelGeneratorService voxelGeneratorService, int maxThreads, int lodCount, int chunkCountY)
    {
        this.voxelStorage = voxelStorage;
        this.chunkCountY = chunkCountY;
        this.lodCount = lodCount;
        geometryGenerator = new GeometryGenerator(voxelStorage.voxelsPerMeter, voxelStorage.chunkVoxelSize, lodCount);
        
        activeColumns = new ConcurrentDictionary<(int, int), ChunkColumnGeometry>();
        taskQueue = new PriorityQueue<ChunkColumnGeometryTask, int>();
        pendingLockedTasks = [];
        taskQueueLock = new Lock();
        eventLock = new Lock();
        taskAvailable = new ManualResetEventSlim(false);
        stopWorkers = false;
        workerThreads = [];
        // subscribe to voxel generator service to receive information about finished columns
        voxelGeneratorService.SubscribeOnChunkColumnGenerated(OnChunkColumnGenerated);
        // initialize threads
        for (int i = 0; i < maxThreads; i++)
        {
            Thread thread = new Thread(WorkerThread)
            {
                IsBackground = true
            };
            workerThreads.Add(thread);
            thread.Start();
        }
    }
    
    /**
     * Subscribes to given voxel edit service instance for column voxelization updates.
     */
    public void SubscribeToVoxelEditService(VoxelEditService voxelEditService)
    {
        voxelEditService.SubscribeOnChunkModified(OnChunkModified);
    }

    /**
     * Returns average time geometry generator spends at generating single chunk lod.
     */
    public List<long> GetAverageChunkLodGenerationTime()
    {
        return geometryGenerator.GetAverageChunkLodGenerationTime();
    }
    
    /**
     * Returns count of remaining ready to process tasks.
     */
    public uint GetRemainingTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)(taskQueue.Count);
        }
    }
    
    /**
     * Returns count of tasks that are waiting for their columns to finished voxelization (including neighbors).
     */
    public uint GetLockedTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)pendingLockedTasks.Count;
        }
    }
    
    /**
     * Returns total number of columns that have their geometry generated or that are being generated right now.
     */
    public uint GetActiveColumnsCount()
    {
        return (uint)activeColumns.Count;
    }
    
    /**
     * Fired to signal that chunk with given coordinates has been updated and needs to have its own
     * and its neighbors (6 total, 4 horizontal, 2 vertical) geometry regenerated.
     * Checks that the necessary columns exist and plans update task.
     */
    private void OnChunkModified(int chunkX, uint chunkY, int chunkZ)
    {
        Vector2Int[] neighborOffsets = [
            new(0, 0),
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        ];
        // check column and 4 neighbors exist and are generated
        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborPosition = new Vector2Int(chunkX, chunkZ) + offset;
            if (!activeColumns.ContainsKey((neighborPosition.x, neighborPosition.y)))
            {
                editingProcessed?.Invoke();
                return;
            }
        }
        
        // plan high priority update task
        lock (taskQueueLock)
        {
            taskQueue.Enqueue(new ChunkColumnGeometryTask(GEOMETRY_TASK_TYPE.UPDATE_EDIT, LodLevel.UNLOADED, chunkX, chunkY, chunkZ, 0), 0);
            taskAvailable.Set();
        }
    }
    
    /**
     * Immediately unloads and cancels planned generation tasks of given set of columns.
     */
    public void UnloadColumns(HashSet<(int chunkX, int chunkZ)> columnsToUnload, LodLevel lodLevel)
    {
        if (columnsToUnload == null || columnsToUnload.Count == 0)
            return;
        
        lock (taskQueueLock)
        {
            // remove pending locked tasks for these columns
            List<ChunkColumnGeometryTask> tasksToRemove = [];
            
            foreach (ChunkColumnGeometryTask pendingTask in pendingLockedTasks)
            {
                if (columnsToUnload.Contains((pendingTask.chunkX, pendingTask.chunkZ)) && pendingTask.lodLevel == lodLevel)
                {
                    tasksToRemove.Add(pendingTask);
                    columnsToUnload.Remove((pendingTask.chunkX, pendingTask.chunkZ));
                }
            }

            foreach (ChunkColumnGeometryTask task in tasksToRemove)
            {
                pendingLockedTasks.Remove(task);
            }
            
            // remove active columns if they are there
            foreach (var column in columnsToUnload)
            {
                if (activeColumns.TryGetValue(column, out var columnGeometry))
                {
                    lock (columnGeometry.lodLock)
                    {
                        // remove supplied lod level
                        columnGeometry.lods[(int)lodLevel] = null;
                    }
                    
                    // might be completely empty - delete it from active columns
                    bool isEmpty = true;
                    for (int lod = 0; lod < lodCount; lod++)
                    {
                        if (columnGeometry.lods[lod] != null)
                        {
                            isEmpty = false;
                            break;
                        }
                    }

                    if (isEmpty)
                    {
                        activeColumns.Remove((column.chunkX, column.chunkZ), out _);
                    }
                }
            }
            
            // remove tasks from queue
            List<ChunkColumnGeometryTask> remainingTasks = [];
            taskAvailable.Reset();
            while (taskQueue.Count > 0)
            {
                ChunkColumnGeometryTask task = taskQueue.Dequeue();
                if (!(columnsToUnload.Contains((task.chunkX, task.chunkZ)) && task.lodLevel == lodLevel))
                {
                    remainingTasks.Add(task);
                }
            }
            
            foreach (ChunkColumnGeometryTask task in remainingTasks)
            {
                taskQueue.Enqueue(task, task.priority);
            }
            if (taskQueue.Count > 0)
            {
                taskAvailable.Set();
            }
        }
    }

    /**
     * Adds normal column generation task.
     * If the column is fully generated (including neighbors) then it is scheduled immediately.
     * Otherwise it needs to wait for completion of voxelization step.
     */
    public void AddNormalTask(ChunkColumnGeometryTask task)
    {
        lock (taskQueueLock)
        {
            // is it ready for geometry generation
            ChunkColumn column = voxelStorage.GetChunkColumn(task.chunkX, task.chunkZ);
            if (column == null || !column.IsFullyGenerated() || !column.HasAllNeighbors())
            {
                pendingLockedTasks.Add(task); // needs to wait
            }
            else
            {
                // can be processed
                taskQueue.Enqueue(task, task.priority);
                taskAvailable.Set();
            }
        }
    }

    /**
     * Returns chunk column geometry for given column coordinates if it exists, or null.
     * Lod readiness must be checked manually.
     */
    public ChunkColumnGeometry GetColumn(int chunkX, int chunkZ)
    {
        if (activeColumns.ContainsKey((chunkX, chunkZ)))
        {
            return activeColumns[(chunkX, chunkZ)];
        }

        return null;
    }
    
    /**
     * Fired to signal that voxel generator service has finished given column.
     * Loops through all pending tasks to find out whether they were unlocked by this event, if yes, plan them.
     */
    private void OnChunkColumnGenerated(ChunkColumn column)
    {
        if (column == null)
        {
            GD.PrintErr("OnChunkColumnGenerated received null column");
            return;
        }
        
        lock (taskQueueLock)
        {
            // check all pending locked tasks whether they can be added now
            List<ChunkColumnGeometryTask> tasksToRemove = [];
            foreach (ChunkColumnGeometryTask task in pendingLockedTasks)
            {
                ChunkColumn taskColumn = voxelStorage.GetChunkColumn(task.chunkX, task.chunkZ);
                if (taskColumn == null || !taskColumn.IsFullyGenerated() || !taskColumn.HasAllNeighbors())
                {
                    continue;
                }
                // add to planned
                taskQueue.Enqueue(task, task.priority);
                taskAvailable.Set();
                tasksToRemove.Add(task);
            }
            
            foreach (ChunkColumnGeometryTask task in tasksToRemove)
            {
                pendingLockedTasks.Remove(task);
            }
        }
    }

    /**
     * Generates single geometry lod for all chunks of given column.
     * Uses geometry generator.
     * Fires columnGeometryGenerated to notify WorldDisplayService.
     */
    private void GenerateColumnAtLod(ChunkColumnGeometry columnGeometry, ColumnGeometryLod columnGeometryLod, LodLevel lodLevel)
    {
        if (columnGeometry == null)
        {
            GD.PrintErr("GenerateColumn received null column geometry");
            return;
        }
        
        ChunkColumn column = voxelStorage.GetChunkColumn(columnGeometry.chunkX, columnGeometry.chunkZ);
        
        if (column == null)
        {
            GD.PrintErr($"Chunk col geometry {columnGeometry.chunkX}, {columnGeometry.chunkZ} has no corresponding chunk column!");
            return;
        }

        if (!column.IsFullyGenerated())
        {
            GD.PrintErr($"Chunk col {columnGeometry.chunkX}, {columnGeometry.chunkZ} is not fully generated yet!");
            return;
        }
        
        if (!column.HasAllNeighbors())
        {
            GD.PrintErr($"Chunk col {columnGeometry.chunkX}, {columnGeometry.chunkZ} does not have all neighbors!");
            return;
        }

        // generate the column at given lod level
        columnGeometryLod.chunks = geometryGenerator.GenerateChunkColumnGeometry(column, lodLevel);
        columnGeometryLod.ready = true;
        lock (eventLock)
        {
            columnGeometryGenerated?.Invoke(columnGeometry);
        }
    }
    
    /**
     * Regenerates geometry for the supplied chunk triplets (column, chunkY, lod)
     * Uses geometry generator.
     * Fires chunkGeometryUpdated for each chunk to notify world display service of chunks to re-display.
     * Fires editingProcessed at the end to re-enable user editing from Root.
     */
    private void RegenerateChunks(List<ChunkUpdateTask> chunkUpdateTasks)
    {
        // regenerates all supplied chunks
        foreach (ChunkUpdateTask updateTask in chunkUpdateTasks)
        {
            if (updateTask.columnGeometry == null)
                continue;
            
            ChunkColumn column = voxelStorage.GetChunkColumn(updateTask.columnGeometry.chunkX, updateTask.columnGeometry.chunkZ);
            if (column == null)
            {
                GD.PrintErr($"RegenerateChunks: column data for ({updateTask.columnGeometry.chunkX}, {updateTask.columnGeometry.chunkZ}) is null");
                continue;
            }
            
            if (!column.IsFullyGenerated())
            {
                GD.PrintErr($"Chunk col {updateTask.columnGeometry.chunkX}, {updateTask.columnGeometry.chunkZ} is not fully generated yet!");
                return;
            }
        
            if (!column.HasAllNeighbors())
            {
                GD.PrintErr($"Chunk col {updateTask.columnGeometry.chunkX}, {updateTask.columnGeometry.chunkZ} does not have all neighbors!");
                return;
            }
            
            // regenerate this chunk
            ChunkGeometry chunkGeometry = geometryGenerator.GenerateChunkGeometry(column, (int)updateTask.chunkY, updateTask.lodLevel);
            lock (updateTask.columnGeometry.lodLock)
            {
                updateTask.columnGeometry.lods[(int)updateTask.lodLevel].chunks[updateTask.chunkY] = chunkGeometry;
            }
            
            lock (eventLock)
            {
                // inform world display service of the need to update this chunk
                chunkGeometryUpdated?.Invoke(updateTask.columnGeometry.chunkX, updateTask.chunkY, updateTask.columnGeometry.chunkZ);
            }
        }
        lock (eventLock)
        {
            // inform root, that changes needed after editing were processed (does not wait for display service)
            editingProcessed?.Invoke();
        }
    }

    /**
     * Work function for each geometry generation thread.
     * Processes scheduled tasks one by one. Both generation and update ones.
     */
    private void WorkerThread()
    {
        while (!stopWorkers)
        {
            ChunkColumnGeometryTask task = null;
            ChunkColumnGeometry columnGeometry = null;
            ColumnGeometryLod columnGeometryLod = null;
            List<ChunkUpdateTask> chunkUpdateTasks = [];
            bool hasTask = false;
            // find work and prepare instances to work on
            lock (taskQueueLock)
            {
               if (taskQueue.Count > 0)
               {
                   task = taskQueue.Dequeue();
                   if (task.type == GEOMETRY_TASK_TYPE.GENERATE)
                   {
                       // generate column geometry
                       if (!activeColumns.ContainsKey((task.chunkX, task.chunkZ))) // it is new
                       {
                           columnGeometry = new ChunkColumnGeometry(task.chunkX, task.chunkZ, lodCount);
                           lock (columnGeometry.lodLock)
                           {
                               columnGeometryLod = new ColumnGeometryLod(chunkCountY);
                               columnGeometry.lods[(int)task.lodLevel] = columnGeometryLod;
                               activeColumns[(task.chunkX, task.chunkZ)] = columnGeometry;
                               hasTask = true;
                           }
                       }
                       else
                       {
                           // already loaded, could be a different lod level
                       
                           columnGeometry = activeColumns[(task.chunkX, task.chunkZ)];
                           lock (columnGeometry.lodLock)
                           {
                               columnGeometryLod = columnGeometry.lods[(int)task.lodLevel];
                               if (columnGeometryLod == null)
                               {
                                   // lod not generated yet
                                   columnGeometryLod = new ColumnGeometryLod(chunkCountY);
                                   columnGeometry.lods[(int)task.lodLevel] = columnGeometryLod;
                                   hasTask = true;
                               }
                           }
                       }
                       
                   }
                   else if (task.type == GEOMETRY_TASK_TYPE.UPDATE_EDIT)
                   {
                       // need to regenerate this chunkY in the given column, together with 4 neighbor chunks horizontally,
                       // and 2 vertically
                       // prepare horizontal offsets
                       List<(Vector2 dir, uint chunkY)> chunkOffsetsToUpdate = [
                           (new Vector2(0, 0), task.chunkY),
                           (new Vector2(1, 0), task.chunkY),
                           (new Vector2(-1, 0), task.chunkY),
                           (new Vector2(0, 1), task.chunkY),
                           (new Vector2(0, -1), task.chunkY)
                       ];
                       // add vertical offsets of the center one
                       if (task.chunkY + 1 < chunkCountY)
                       {
                           chunkOffsetsToUpdate.Add((new Vector2(0, 0), task.chunkY + 1));
                       }
                       if (task.chunkY > 0)
                       {
                           chunkOffsetsToUpdate.Add((new Vector2(0, 0), task.chunkY - 1));
                       }
                       
                       // prepare instances for the RegenerateChunks function
                       foreach (var offset in chunkOffsetsToUpdate)
                       {
                           int neighborChunkX = task.chunkX + (int)offset.dir.X;
                           int neighborChunkZ = task.chunkZ + (int)offset.dir.Y;
                           if (activeColumns.ContainsKey((neighborChunkX, neighborChunkZ)))
                           {
                               // sets up update tasks (column, y, lod level)
                               ChunkColumnGeometry neighborColumn = activeColumns[(neighborChunkX, neighborChunkZ)];
                               lock (neighborColumn.lodLock)
                               {
                                   for (int lod = 0; lod < lodCount; lod++)
                                   {
                                       if (neighborColumn.lods[lod] != null && neighborColumn.lods[lod].ready)
                                       {
                                           chunkUpdateTasks.Add(new ChunkUpdateTask(neighborColumn, offset.chunkY, (LodLevel)lod));
                                       }
                                   }
                               }
                           }
                       }

                       if (chunkUpdateTasks.Count > 0)
                       {
                           hasTask = true;
                       }
                   }
               }
            }
            // process the task
            if (hasTask)
            {
                switch (task.type)
                {
                    case GEOMETRY_TASK_TYPE.GENERATE:
                        GenerateColumnAtLod(columnGeometry, columnGeometryLod, task.lodLevel);
                        break;
                    case GEOMETRY_TASK_TYPE.UPDATE_EDIT:
                        RegenerateChunks(chunkUpdateTasks);
                        break;
                }
            }
            else
            {
                taskAvailable.Wait(100);
                taskAvailable.Reset();
            }
        }
    }
    
    /**
     * Waits for closure of all threads.
     */
    public void Dispose()
    {
        stopWorkers = true;
        taskAvailable.Set();
        foreach (Thread thread in workerThreads)
        {
            if (thread.IsAlive)
                thread.Join();
        }
        taskAvailable.Dispose();
    }

    /**
     * Subscribes the handler to information about generated columns.
     */
    public void SubscribeOnColumnGeometryGenerated(Action<ChunkColumnGeometry> handler)
    {
        lock (eventLock)
        {
            columnGeometryGenerated += handler;
        }
    }
    
    /**
     * Subscribes the handler to information about updated chunk geometry.
     */
    public void SubscribeOnChunkGeometryUpdated(Action<int, uint, int> handler)
    {
        lock (eventLock)
        {
            chunkGeometryUpdated += handler;
        }
    }
    
    /**
     * Subscribes the handler to information that editing request was fully processed.
     */
    public void SubscribeOnEditingProcessed(Action handler)
    {
        lock (eventLock)
        {
            editingProcessed += handler;
        }
    }
}
