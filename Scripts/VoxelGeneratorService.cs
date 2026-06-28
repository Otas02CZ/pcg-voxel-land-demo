// FILE: VoxelGeneratorService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains service class that manages voxelization of world regions into chunk columns.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents single column voxelization task for the voxel generator service to process.
 */
public struct ChunkColumnTask(int chunkX, int chunkZ, int priority)
{
    public readonly int chunkX = chunkX;
    public readonly int chunkZ = chunkZ;
    public readonly int priority = priority; // lower value means higher priority
}

/**
 * Manages voxelization of chunk columns in voxel storage from data in their world regions.
 * Internally uses VoxelGenerator to generate each column. 
 * Processes Root issued generation tasks in parallel with up to the specified count of threads.
 * Unloading is done immediately in batches via the UnloadColumns function, but it only acts as cancellation
 * of tasks, as the actual voxelized ChunkColumn structures are managed from Root itself.
 * Before a column can be generated, it needs regions where it and its 8 neighbors belong to be generated.
 * Before signaling completion of a column, it firstly needs to have its 4 neighbors generated as well
 * - necessary for geometry generator to correctly generate geometry at column boundaries.
 */
public class VoxelGeneratorService : IDisposable
{
    private readonly VoxelGenerator voxelGenerator;
    private readonly WorldGeneratorService worldGeneratorService;
    
    private readonly int metersPerRegion;
    private readonly int metersPerChunk;
    
    private readonly HashSet<(int chunkX, int chunkZ)> generatedColumns; // needed to correctly track generated and pending columns for task cancellation
    // unloading of voxel storage chunk columns handles Root on its own
    private readonly PriorityQueue<ChunkColumnTask, int> taskQueueReady;
    private readonly List<ChunkColumnTask> pendingChunkColumnTasks; // chunk column tasks, waiting for their region and neighboring regions generation
    private readonly List<ChunkColumn> chunkColumnsAwaitNeighConfirm; // chunk columns that are generated but waiting for neighbor generation so that their geometry can be generated
    private readonly Lock taskQueueLock; // lock for access to task lists and queues
    private readonly Lock eventLock;

    private Action<ChunkColumn> chunkColumnGenerated; // fires when column and its neighbors are fully generated
    private readonly List<Thread> workerThreads;
    private readonly ManualResetEventSlim taskAvailable; // signals task availability to startup inactive threads
    private volatile bool stopWorkers;
    
    public VoxelGeneratorService(int threads, VoxelGeneratorParams voxelGenParams)
    {
        voxelGenerator = new VoxelGenerator(voxelGenParams);
        worldGeneratorService = voxelGenParams.worldGeneratorService;
        
        metersPerRegion  = voxelGenParams.metersPerRegion;
        metersPerChunk = voxelGenParams.metersPerChunk;
        
        workerThreads = new List<Thread>(threads);
        taskAvailable = new ManualResetEventSlim(false);
        generatedColumns = [];
        taskQueueReady = new PriorityQueue<ChunkColumnTask, int>();
        chunkColumnsAwaitNeighConfirm = [];
        pendingChunkColumnTasks = [];
        taskQueueLock = new Lock();
        eventLock = new Lock();
        // subscribe to world generator service to receive region updates unlocking its work
        worldGeneratorService.SubscribeOnRegionGenerated(OnWorldRegionGenerated);
        
        // start worker threads
        for (int i = 0; i < threads; i++)
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
     * Passes stalactite models to voxel generator
     */
    public void PassModelsToVoxelGenerator(ConcurrentBag<Stalactite> stalactiteModels)
    {
        voxelGenerator.AddStalactiteModels(stalactiteModels);
    }
    
    /**
     * Immediately unregisters and cancels generation tasks for given chunk columns.
     * Unloading of voxel storage chunk columns themselves is not done here, but managed directly from Root.
     */
    public void UnloadColumns(HashSet<(int chunkX, int chunkZ)> columnsToUnload)
    {
        if (columnsToUnload == null || columnsToUnload.Count == 0)
            return;
        
        // copy for neighbor columns check
        HashSet<(int chunkX, int chunkZ)> columnsToUnloadCopy = new HashSet<(int chunkX, int chunkZ)>(columnsToUnload);

        // remove from pending, active or plan cancellation for these columns
        lock (taskQueueLock)
        {
            // remove pending tasks for these columns
            List<ChunkColumnTask> tasksToRemove = [];
            foreach (ChunkColumnTask pendingTask in pendingChunkColumnTasks)
            {
                if (columnsToUnload.Contains((pendingTask.chunkX, pendingTask.chunkZ)))
                {
                    tasksToRemove.Add(pendingTask);
                    columnsToUnload.Remove((pendingTask.chunkX, pendingTask.chunkZ));
                }
            }
            
            foreach (ChunkColumnTask task in tasksToRemove)
            {
                pendingChunkColumnTasks.Remove(task);
            }
            
            // remove registered generated columns
            foreach (var column in columnsToUnload)
            {
                if (generatedColumns.Contains(column))
                {
                    generatedColumns.Remove(column);
                }
            }
            
            // remove tasks from queue
            List<ChunkColumnTask> remainingTasks = [];
            taskAvailable.Reset();
            while (taskQueueReady.Count > 0)
            {
                ChunkColumnTask task = taskQueueReady.Dequeue();
                if (!columnsToUnload.Contains((task.chunkX, task.chunkZ)))
                {
                    remainingTasks.Add(task);
                }
            }
            
            foreach (ChunkColumnTask task in remainingTasks)
            {
                taskQueueReady.Enqueue(task, task.priority);
                taskAvailable.Set();
            }
            if (taskQueueReady.Count > 0)
            {
                taskAvailable.Set();
            }
        }
        
        // remove from neighbor wait list
        lock (eventLock)
        {
            List<ChunkColumn> toRemove = [];
            foreach (ChunkColumn chunkColumn in chunkColumnsAwaitNeighConfirm)
            {
                if (columnsToUnloadCopy.Contains((chunkColumn.chunkX, chunkColumn.chunkZ)))
                {
                    toRemove.Add(chunkColumn);
                }
            }

            foreach (ChunkColumn chunkColumn in toRemove)
            {
                chunkColumnsAwaitNeighConfirm.Remove(chunkColumn);
            }
        }
    }
    
    /**
     * Returns count of remaining tasks that are ready to process.
     */
    public uint GetRemainingTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)(taskQueueReady.Count);
        }
    }
    
    /**
     * Returns count of tasks that are waiting for generation of their or 8-neighbor regions.
     */
    public uint GetLockedTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)pendingChunkColumnTasks.Count;
        }
    }
    
    /**
     * Returns count of columns that are waiting for their 4-neighborhood to generate,
     * before they will be signaled to geometry generation.
     */
    public uint GetWaitingNeighborsCount()
    {
        lock (eventLock)
        {
            return (uint)chunkColumnsAwaitNeighConfirm.Count;
        }
    }

    /**
     * Returns total count of registered columns.
     */
    public uint GetActiveColumnsCount()
    {
        lock (taskQueueLock)
        {
            return (uint)generatedColumns.Count;
        }
    }

    /**
     * Adds generation task for specified column.
     * If its and its 8-neighbor regions are generated, it is scheduled to be processed.
     * Otherwise it must wait.
     */
    public void AddTask(ChunkColumnTask task)
    {
        lock (taskQueueLock)
        {
            // loop through 8 neighbor columns
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    // check whether their region is ready
                    int colRegionX = (int)Math.Floor((task.chunkX + dx) * metersPerChunk / (float)metersPerRegion);
                    int colRegionZ = (int)Math.Floor((task.chunkZ + dz) * metersPerChunk / (float)metersPerRegion);
                    WorldRegion colRegion = worldGeneratorService.GetRegionWhenReady(colRegionX, colRegionZ);
                    if (colRegion == null)
                    {
                        // region not loaded, add to waiting list
                        pendingChunkColumnTasks.Add(task);
                        return;
                    }
                }
            }
            
            // region is loaded, add to ready queue
            taskQueueReady.Enqueue(task, task.priority);
            taskAvailable.Set();
        }
    }
    
    /**
     * Fired to signal that a given world region has been generated.
     * Checks whether any of the pending tasks could now be scheduled by checking whether theirs and their neighbor regions are ready.
     */
    private void OnWorldRegionGenerated(WorldRegion worldRegion)
    {
        lock (taskQueueLock)
        {
            // loop through all pending tasks
            List<ChunkColumnTask> toRemove = [];
            foreach (ChunkColumnTask task in pendingChunkColumnTasks)
            {
                bool canBeGenerated = true;
                // check whether regions of it itself and its 8 neighbors are ready
                for (int dx = -1; dx <= 1 && canBeGenerated; dx++)
                {
                    for (int dz = -1; dz <= 1 && canBeGenerated; dz++)
                    {
                        // check region of this column
                        int colRegionX = (int)Math.Floor((task.chunkX + dx) * metersPerChunk / (float)metersPerRegion);
                        int colRegionZ = (int)Math.Floor((task.chunkZ + dz) * metersPerChunk / (float)metersPerRegion);
                        WorldRegion colRegion = worldGeneratorService.GetRegionWhenReady(colRegionX, colRegionZ);
                        if (colRegion == null)
                        {
                            canBeGenerated = false;
                        }
                    }
                }
                // this one can be generated
                if (canBeGenerated)
                {
                    taskQueueReady.Enqueue(task, task.priority);
                    taskAvailable.Set();
                    toRemove.Add(task);
                }
            }
            foreach (ChunkColumnTask task in toRemove)
            {
                pendingChunkColumnTasks.Remove(task);
            }
        }
    }
    
    /**
     * Fired when a single column finishes its own voxelization process.
     * Checks whether this event has influenced any of the columns that are generated but were
     * waiting for any of their 4 neighbor columns to generate before they can be signaled to geometry generation.
     */
    private void TestPendingColumnNeighborsReady()
    {
        List<ChunkColumn> toRemove = [];
        // loop through all waiting columns
        foreach (ChunkColumn chunkColumn in chunkColumnsAwaitNeighConfirm)
        {
            if (chunkColumn == null)
            {
                continue;
            }
            
            bool columnIsReady = true;
            
            // check whether its neighbors exist and are ready
            if (chunkColumn.neighbors == null || chunkColumn.neighbors.Length != 4)
            {
                columnIsReady = false;
            }
            else
            {
                foreach (ChunkColumn neighbor in chunkColumn.neighbors)
                {
                    if (neighbor == null || !neighbor.isGenerated)
                    {
                        columnIsReady = false;
                        break;
                    }
                }
            }
            // signal to geometry generator if it is ready
            if (columnIsReady)
            {
                toRemove.Add(chunkColumn);
                lock (eventLock)
                {
                    chunkColumnGenerated?.Invoke(chunkColumn);
                }
            }
        }
        
        foreach (ChunkColumn chunkColumn in toRemove)
        {
            chunkColumnsAwaitNeighConfirm.Remove(chunkColumn);
        }
    }
    
    /**
     * Work loop of each thread.
     * Processes generation tasks one by one.
     */
    private void WorkerThread()
    {
        while (!stopWorkers)
        {
            // obtain task
            ChunkColumnTask task = default;
            bool hasTask = false;
            lock (taskQueueLock)
            {
                if (taskQueueReady.Count > 0)
                {
                    task = taskQueueReady.Dequeue();
                    if (!generatedColumns.Contains((task.chunkX, task.chunkZ)))
                    {
                        generatedColumns.Add((task.chunkX, task.chunkZ));
                        hasTask = true;
                    }
                }
            }

            // process obtained task
            if (hasTask)
            {
                ChunkColumn generatedColumn = voxelGenerator.GenerateChunkColumn(task);
                if (generatedColumn == null)
                    continue;
                // test neighbors and potentially invoke ready signal
                lock (eventLock)
                {
                    chunkColumnsAwaitNeighConfirm.Add(generatedColumn);
                    TestPendingColumnNeighborsReady();
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
     * Waits for closure of all available threads.
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
     * Subscribes given handler to this service chunkColumnGenerated signal.
     */
    public void SubscribeOnChunkColumnGenerated(Action<ChunkColumn> handler)
    {
        lock (eventLock)
        {
            chunkColumnGenerated += handler;
        }
    }
}
