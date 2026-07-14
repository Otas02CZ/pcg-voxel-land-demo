// FILE: WorldDisplayService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains Service class that displays and hides chunk geometry in the world in the engine. 

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Types of tasks for the world display service.
 */
public enum WorldDisplayTaskType : byte
{
    DISPLAY,
    HIDE,
    UPDATE_EDIT,
}

/**
 * Manages information of a single currently displayed column.
 * Necessary for keeping track of active columns and their changes / unloading.
 */
public class DisplayedColumn(int chunkX, int chunkZ, LodLevel currentLod, int chunkCountY)
{
    public int chunkX = chunkX;
    public int chunkZ = chunkZ;
    public LodLevel currentLod = currentLod;
    public readonly Vector3Int[] realPositions = new Vector3Int[chunkCountY];
    public readonly MeshInstance3D[] chunkInstances = new MeshInstance3D[chunkCountY];
    public readonly StaticBody3D[] chunkCollisions = new StaticBody3D[chunkCountY];
}

/**
 * Represents task for the display service to process.
 */
public class WorldDisplayTask(WorldDisplayTaskType type, int chunkX, uint chunkY, int chunkZ, LodLevel lodLevel)
{
    public readonly WorldDisplayTaskType type = type;
    public readonly int chunkX = chunkX;
    public readonly uint chunkY = chunkY;
    public readonly int chunkZ = chunkZ;
    public readonly LodLevel lodLevel = lodLevel;
}

public class PreProcessedChunk(ArrayMesh arrayMesh, Vector3[] collisionGeometry, Vector3Int worldPosition)
{
    public readonly ArrayMesh arrayMesh = arrayMesh;
    public readonly Vector3[] collisionGeometry = collisionGeometry;
    public readonly Vector3Int worldPosition = worldPosition;
}

public class PreProcessedChunkColumn(PreProcessedChunk[] chunks, WorldDisplayTask task)
{
    public readonly PreProcessedChunk[] chunks = chunks;
    public readonly WorldDisplayTask task = task;
}

/**
 * Chunk column geometry display, update and unload service for displaying world voxel geometry in the Godot Engine.
 * Includes support for LOD changes.
 * Controlled by a task system. Display (includes LOD changes) and changes due to user voxel editing are run on a single task basis.
 * Complete unloading of columns is done in batches when all unload operations are done at once.
 * Does not process tasks on its own via threading as other services do. But exposes the jobs to be done from outside by main thread
 * from root so that mesh changes in Godot scene happen correctly on the main thread.
 * Supports repositioning of column geometry to engine world center to eliminate floating point errors during rendering and physics.
 * TODO: try rework with async processing
 * TODO rewrite
 */
public class WorldDisplayService
{
    private readonly Dictionary<(int, int), DisplayedColumn> displayedColumns; // currently displayed columns
    private readonly List<WorldDisplayTask> taskList; // remaining tasks, that can be processed in next invocations of ProcessDisplayUpdateTask
    private readonly List<WorldDisplayTask> notReadyTasks; // tasks scheduled by root, but waiting for geometry or lower steps of generation
    private readonly Lock taskAccessLock;

    private readonly List<(int chunkX, int chunkZ)> columnsToHide; // columns to unload in next batched removal
    
    // pre-processing data and thread management
    private readonly int maxPreProcessedColumns = 100;
    private readonly List<PreProcessedChunkColumn> preProcessedColumns;
    private Thread processingThread;
    private volatile bool stopThread = false;
    private const int threadSleepMs = 100;
    
    // dependencies
    private readonly GeometryGeneratorService geometryGeneratorService;
    private readonly ShaderMaterial waterMaterial;
    private readonly ShaderMaterial blockMaterialVariance;
    private readonly StandardMaterial3D blockMaterialBasic;
    private readonly Node3D voxelWorld;
    private readonly PackedScene collisionScene;
        
    private readonly int chunkCountY;
    
    // current origin shifting offset
    private Vector3Int originShiftOffsetXZ;
    
    public WorldDisplayService(GeometryGeneratorService geometryGeneratorService, int chunkCountY, ShaderMaterial waterMaterial, StandardMaterial3D blockMaterialBasic, ShaderMaterial blockMaterialVariance, Node3D voxelWorld)
    {
        displayedColumns = new Dictionary<(int, int), DisplayedColumn>();
        taskList = [];
        columnsToHide = [];
        notReadyTasks = [];
        taskAccessLock = new Lock();
        collisionScene = GD.Load<PackedScene>("res://Scenes/ChunkCollision.tscn");
        
        preProcessedColumns = [];
        processingThread = new Thread(PreProcessingWorkThread);
        processingThread.Start();
        
        this.geometryGeneratorService = geometryGeneratorService;
        this.chunkCountY = chunkCountY;
        this.voxelWorld = voxelWorld;
        this.waterMaterial = waterMaterial;
        this.blockMaterialBasic = blockMaterialBasic;
        this.blockMaterialVariance = blockMaterialVariance;
        
        originShiftOffsetXZ = new Vector3Int(0, 0, 0);
        // subscribe to signals
        geometryGeneratorService.SubscribeOnColumnGeometryGenerated(OnChunkColumnGeometryReady);
        geometryGeneratorService.SubscribeOnChunkGeometryUpdated(OnChunkGeometryUpdated);
    }

    /**
     * TODO description
     */
    private void PreProcessingWorkThread()
    {
        while (!stopThread)
        {
            if (preProcessedColumns.Count >= maxPreProcessedColumns)
            {
                Thread.Sleep(threadSleepMs);
                continue;
            }
            
            // try to obtain the first task
            WorldDisplayTask task = null;
            lock (taskAccessLock)
            {
                if (taskList.Count > 0)
                {
                    task = taskList[0];
                    taskList.RemoveAt(0);
                }
                
                // need to prepare the display column as well
                if (task != null && !displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                {
                    DisplayedColumn displayedColumn = new DisplayedColumn(task.chunkX, task.chunkZ, LodLevel.UNLOADED, chunkCountY);
                    displayedColumns.Add((task.chunkX, task.chunkZ), displayedColumn);
                }
            }

            if (task == null)
            {
                Thread.Sleep(threadSleepMs);
                continue;
            }

            switch (task.type)
            {
                // pre-processing data for display of a new column or update of existing one to different LOD
                case WorldDisplayTaskType.DISPLAY:
                {
                    // obtain its geometry from geometry service
                    ChunkColumnGeometry columnGeometry = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                    if (columnGeometry == null)
                    {
                        GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null geometry");
                        continue;
                    }

                    ColumnGeometryLod geometry;
                    ColumnGeometryLod collisionGeometry;

                    lock (columnGeometry.lodLock)
                    {
                        // check that necessary lods are present, lod0 must have lod2 for collision
                        if (columnGeometry.lods[(int)task.lodLevel] == null || !columnGeometry.lods[(int)task.lodLevel].ready ||
                            (task.lodLevel == LodLevel.LOD0 && (columnGeometry.lods[(int)LodLevel.LOD2] == null || !columnGeometry.lods[(int)LodLevel.LOD2].ready))
                           )
                        {
                            GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null required lod");
                            continue;
                        }

                        geometry = columnGeometry.lods[(int)task.lodLevel];
                        collisionGeometry = columnGeometry.lods[(int)LodLevel.LOD2];
                    }
                    
                    PreProcessedChunk[] preProcessedChunks = new PreProcessedChunk[chunkCountY];
                    
                    // process all chunks
                    for (int y = 0; y < chunkCountY; y++)
                    {
                        if (!geometry.chunks[y].hasGeometry)
                        {
                            continue; // might not have any geometry
                        }

                        ChunkGeometry chunkGeometry = geometry.chunks[y];
                        bool voxelVariance = task.lodLevel == LodLevel.LOD0; // only LOD0 has variance shader for non water blocks
                        ArrayMesh arrayMesh = CreateChunkArrayMesh(chunkGeometry, voxelVariance);
                        
                        // also prepare collision geometry for lod0
                        Vector3[] collisionGeometryData = null;
                        
                        if (task.lodLevel == LodLevel.LOD0)
                        {
                            chunkGeometry = collisionGeometry.chunks[y];
                            int indicesLength = chunkGeometry.hasGeometry ? chunkGeometry.indices.Length : 0;
                            int waterIndicesLength = chunkGeometry.hasWaterGeometry ? chunkGeometry.waterIndices.Length : 0;
                            int totalLength = indicesLength + waterIndicesLength;

                            if (totalLength > 0)
                            {
                                collisionGeometryData = new Vector3[totalLength];
                                if (chunkGeometry.hasGeometry)
                                {
                                    for (int i = 0; i < indicesLength; i++)
                                    {
                                        collisionGeometryData[i] = chunkGeometry.vertices[chunkGeometry.indices[i]];
                                    }
                                }

                                if (chunkGeometry.hasWaterGeometry)
                                {
                                    for (int i = 0; i < waterIndicesLength; i++)
                                    {
                                        collisionGeometryData[i + indicesLength] = chunkGeometry.waterVertices[chunkGeometry.waterIndices[i]];
                                    }
                                }
                            }
                        }
                        
                        preProcessedChunks[y] = new PreProcessedChunk(arrayMesh, collisionGeometryData, chunkGeometry.worldPosition);
                    }
                    
                    PreProcessedChunkColumn preProcessedColumn = new PreProcessedChunkColumn(preProcessedChunks, task);
                    preProcessedColumns.Add(preProcessedColumn);
                    break;
                }
                
                // pre-processing data for update of a single chunk of a column, after it was edited by user
                case WorldDisplayTaskType.UPDATE_EDIT:
                {
                    // obtain its geometry from geometry service
                    ChunkColumnGeometry columnGeometry = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                    if (columnGeometry == null)
                    {
                        GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null geometry");
                        continue;
                    }
            
                    ColumnGeometryLod geometry;
                    ColumnGeometryLod collisionGeometry;
                    
                    // lod level is the same as was before
                    DisplayedColumn displayedColumn = displayedColumns[(task.chunkX, task.chunkZ)];
                    LodLevel lod = displayedColumn.currentLod;
                    if (lod == LodLevel.UNLOADED) // should not happen
                    {
                        continue;
                    }

                    lock (columnGeometry.lodLock)
                    {
                        // check that necessary lods are present, lod0 must have lod2 for collision
                        if (columnGeometry.lods[(int)lod] == null || !columnGeometry.lods[(int)lod].ready ||
                            (lod == LodLevel.LOD0 && (columnGeometry.lods[(int)LodLevel.LOD2] == null || !columnGeometry.lods[(int)LodLevel.LOD2].ready))
                           )
                        {
                            GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null required lod");
                            continue;
                        }

                        geometry = columnGeometry.lods[(int)lod];
                        collisionGeometry = columnGeometry.lods[(int)LodLevel.LOD2];
                    }
                    /*
                    if (!geometry.chunks[task.chunkY].hasGeometry)
                    {
                        continue; // might not have any geometry
                    }
                    */

                    ChunkGeometry chunkGeometry = geometry.chunks[task.chunkY];
                    bool voxelVariance = lod == LodLevel.LOD0; // only LOD0 has variance shader for non water blocks
                    ArrayMesh arrayMesh = CreateChunkArrayMesh(chunkGeometry, voxelVariance);

                    // also prepare collision geometry for lod0
                    Vector3[] collisionGeometryData = null;

                    if (lod == LodLevel.LOD0)
                    {
                        chunkGeometry = collisionGeometry.chunks[task.chunkY];
                        int indicesLength = chunkGeometry.hasGeometry ? chunkGeometry.indices.Length : 0;
                        int waterIndicesLength = chunkGeometry.hasWaterGeometry ? chunkGeometry.waterIndices.Length : 0;
                        int totalLength = indicesLength + waterIndicesLength;

                        if (totalLength > 0)
                        {
                            collisionGeometryData = new Vector3[totalLength];
                            if (chunkGeometry.hasGeometry)
                            {
                                for (int i = 0; i < indicesLength; i++)
                                {
                                    collisionGeometryData[i] = chunkGeometry.vertices[chunkGeometry.indices[i]];
                                }
                            }

                            if (chunkGeometry.hasWaterGeometry)
                            {
                                for (int i = 0; i < waterIndicesLength; i++)
                                {
                                    collisionGeometryData[i + indicesLength] = chunkGeometry.waterVertices[chunkGeometry.waterIndices[i]];
                                }
                            }
                        }
                    }

                    // only a single chunk is being updated
                    PreProcessedChunk[] preProcessedChunks = new PreProcessedChunk[1];
                    preProcessedChunks[0] = new PreProcessedChunk(arrayMesh, collisionGeometryData, chunkGeometry.worldPosition);
                    
                    PreProcessedChunkColumn preProcessedColumn = new PreProcessedChunkColumn(preProcessedChunks, task);
                    preProcessedColumns.Add(preProcessedColumn);
                    break;
                }
            }
        }
    }

    /**
     * Processes signal, that a certain chunk column has now available geometry.
     * Checks whether this column was supposed to be displayed and is waiting for this event.
     * If yes adds it to tasks that can be processed.
     */
    private void OnChunkColumnGeometryReady(ChunkColumnGeometry chunkColumnGeometry)
    {
        lock (taskAccessLock)
        {
            // there can be multiple tasks for a single column - display, display with diff LOD, ...
            List<WorldDisplayTask> tasksToRemove = [];
            foreach (WorldDisplayTask task in notReadyTasks)
            {
                if (task.chunkX == chunkColumnGeometry.chunkX && task.chunkZ == chunkColumnGeometry.chunkZ)
                {
                    lock (chunkColumnGeometry.lodLock)
                    {
                        // need to check that required lods are present, lod0 also requires lod2 for collisions
                        if (chunkColumnGeometry.lods[(int)task.lodLevel] == null || !chunkColumnGeometry.lods[(int)task.lodLevel].ready || 
                            (task.lodLevel == LodLevel.LOD0 && (chunkColumnGeometry.lods[(int)LodLevel.LOD2] == null || !chunkColumnGeometry.lods[(int)LodLevel.LOD2].ready))
                           )
                        {
                            continue;
                        }
                    }
                    
                    taskList.Add(task);
                    tasksToRemove.Add(task);
                }
            }
            foreach (WorldDisplayTask task in tasksToRemove)
            {
                notReadyTasks.Remove(task);
            }
        }
    }

    /**
     * Processes signal that specified chunk was edited by user and has changed geometry,
     * that needs to be swapped with what is currently being shown.
     * Plans update task.
     */
    private void OnChunkGeometryUpdated(int chunkX, uint chunkY, int chunkZ)
    {
        // check whether its column is displayed and so its geometry must be changed
        if (!displayedColumns.ContainsKey((chunkX, chunkZ)))
        {
            return;
        }
        
        // plan the task to update the chunk mesh
        WorldDisplayTask updateTask = new WorldDisplayTask(WorldDisplayTaskType.UPDATE_EDIT, chunkX, chunkY, chunkZ, LodLevel.UNLOADED);
        lock (taskAccessLock)
        {
            taskList.Insert(0, updateTask);
        }
    }
    
    /**
     * Creates ArrayMesh with assembled surfaces of given ChunkGeometry.
     */
    private ArrayMesh CreateChunkArrayMesh(ChunkGeometry geometryData, bool enableVoxelVariance)
    {
        try
        {
            ArrayMesh arrayMesh = new();
            bool hasAnySurface = false;

            // surface 0 - solid blocks
            if (geometryData.hasGeometry && geometryData.vertices?.Length > 0)
            {
                hasAnySurface = true;

                // extend compacted normals and uv arrays
                int baseIndex;
                Vector3[] extendedNormals = new Vector3[geometryData.vertices.Length];
                for (int i = 0; i < geometryData.normals.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedNormals[baseIndex] = geometryData.normals[i];
                    extendedNormals[baseIndex + 1] = geometryData.normals[i];
                    extendedNormals[baseIndex + 2] = geometryData.normals[i];
                    extendedNormals[baseIndex + 3] = geometryData.normals[i];
                }

                Vector2[] extendedUVs = new Vector2[geometryData.vertices.Length];
                for (int i = 0; i < geometryData.uvs.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedUVs[baseIndex] = geometryData.uvs[i];
                    extendedUVs[baseIndex + 1] = geometryData.uvs[i];
                    extendedUVs[baseIndex + 2] = geometryData.uvs[i];
                    extendedUVs[baseIndex + 3] = geometryData.uvs[i];
                }
                
                // build engine expected array structure
                Godot.Collections.Array solidArrays = new();
                solidArrays.Resize((int)Mesh.ArrayType.Max);
                solidArrays[(int)Mesh.ArrayType.Vertex] = geometryData.vertices;
                solidArrays[(int)Mesh.ArrayType.Normal] = extendedNormals;
                solidArrays[(int)Mesh.ArrayType.TexUV] = extendedUVs;
                solidArrays[(int)Mesh.ArrayType.Index] = geometryData.indices;

                Mesh.ArrayFormat arrayFormat = Mesh.ArrayFormat.FormatVertex | Mesh.ArrayFormat.FormatNormal | Mesh.ArrayFormat.FormatTexUV | Mesh.ArrayFormat.FormatIndex;

                // add the created block surface
                arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, solidArrays, flags:arrayFormat);
                
                // assign the block material
                if (enableVoxelVariance)
                {
                    arrayMesh.SurfaceSetMaterial(0, blockMaterialVariance);
                }
                else
                {
                    arrayMesh.SurfaceSetMaterial(0, blockMaterialBasic);
                }
            }

            // surface 1 - water
            if (geometryData.hasWaterGeometry && geometryData.waterVertices?.Length > 0)
            {
                hasAnySurface = true;

                // extend compacted array of normals
                int baseIndex;
                Vector3[] extendedNormals = new Vector3[geometryData.waterVertices.Length];
                for (int i = 0; i < geometryData.waterNormals.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedNormals[baseIndex] = geometryData.waterNormals[i];
                    extendedNormals[baseIndex + 1] = geometryData.waterNormals[i];
                    extendedNormals[baseIndex + 2] = geometryData.waterNormals[i];
                    extendedNormals[baseIndex + 3] = geometryData.waterNormals[i];
                }

                // build array for engine
                Godot.Collections.Array waterArrays = new();
                waterArrays.Resize((int)Mesh.ArrayType.Max);
                waterArrays[(int)Mesh.ArrayType.Vertex] = geometryData.waterVertices;
                waterArrays[(int)Mesh.ArrayType.Normal] = extendedNormals;
                waterArrays[(int)Mesh.ArrayType.Index] = geometryData.waterIndices;

                Mesh.ArrayFormat arrayFormat = Mesh.ArrayFormat.FormatVertex | Mesh.ArrayFormat.FormatNormal | Mesh.ArrayFormat.FormatIndex;
                // add water surface
                arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, waterArrays, flags:arrayFormat);

                // assign it water shader material
                int waterSurfaceIndex = arrayMesh.GetSurfaceCount() - 1;
                arrayMesh.SurfaceSetMaterial(waterSurfaceIndex, waterMaterial);
            }

            if (!hasAnySurface)
            {
                // no geometry to render
                return null;
            }

            return arrayMesh;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to create combined mesh for chunk {geometryData.chunkX},{geometryData.chunkY},{geometryData.chunkZ}: {ex.Message}");
            return null;
        }
    }
    
    /**
     * Whether colum with given coordinates is currently being displayed.
     */
    public bool IsColumnDisplayed(int chunkX, int chunkZ)
    {
        return displayedColumns.ContainsKey((chunkX, chunkZ));
    }
    
    /**
     * Returns total count of displayed columns.
     */
    public uint GetDisplayedColumnsCount()
    {
        return (uint)displayedColumns.Count;
    }
    
    /**
     * Returns count of ready to process application / update tasks.
     */
    public uint GetRemainingTasksCount()
    {
        lock (taskAccessLock)
        {
            return (uint)taskList.Count;
        }
    }
    
    /**
     * Returns count of tasks that are waiting for completion of their generation process.
     */
    public uint GetLockedTasksCount()
    {
        lock (taskAccessLock)
        {
            return (uint)notReadyTasks.Count;
        }
    }
    
    /**
     * Schedules DISPLAY or HIDE task to be processed.
     */
    public void AddTask(WorldDisplayTask task)
    {
        List<WorldDisplayTask> tasksToRemove = [];
        
        lock (taskAccessLock)
        {
            switch (task.type)
            {
                case WorldDisplayTaskType.DISPLAY:
                    // existing tasks for this column must be removed, both not ready and ready
                    foreach (WorldDisplayTask t in notReadyTasks) // not ready
                    {
                        if (t.chunkX == task.chunkX && t.chunkZ == task.chunkZ && t.type == WorldDisplayTaskType.DISPLAY)
                        {
                            tasksToRemove.Add(t);
                        }
                    }
                    foreach (WorldDisplayTask t in tasksToRemove)
                    {
                        notReadyTasks.Remove(t);
                    }
                    tasksToRemove.Clear();
                    foreach (WorldDisplayTask t in taskList) // ready
                    {
                        if (t.chunkX == task.chunkX && t.chunkZ == task.chunkZ && t.type == WorldDisplayTaskType.DISPLAY)
                        {
                            tasksToRemove.Add(t);
                        }
                    }

                    foreach (WorldDisplayTask t in tasksToRemove)
                    {
                        taskList.Remove(t);
                    }
                    
                    // if column is ready add it to tasks that can be processed, otherwise it must wait
                    // for lod0 - lod0 and lod2 must be ready, otherwise only the lod specified
                    ChunkColumnGeometry columnGeometry = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                    if (columnGeometry == null)
                    {
                        notReadyTasks.Add(task);
                        break;
                    }
                    
                    // check lods ready
                    lock (columnGeometry.lodLock)
                    {
                        if (columnGeometry.lods[(int)task.lodLevel] == null || !columnGeometry.lods[(int)task.lodLevel].ready ||
                            (task.lodLevel == LodLevel.LOD0 && (columnGeometry.lods[(int)LodLevel.LOD2] == null || !columnGeometry.lods[(int)LodLevel.LOD2].ready))
                           )
                        {
                            // not ready
                            notReadyTasks.Add(task);
                            break;
                        }
                    }
                    
                    // otherwise ready
                    taskList.Add(task);
                    break;
                case WorldDisplayTaskType.HIDE:
                    // remove from notReadyTasks if it is present
                    foreach (WorldDisplayTask t in notReadyTasks)
                    {
                        if (t.chunkX == task.chunkX && t.chunkZ == task.chunkZ && t.type == WorldDisplayTaskType.DISPLAY)
                        {
                            tasksToRemove.Add(t);
                        }
                    }
                    foreach (WorldDisplayTask t in tasksToRemove)
                    {
                        notReadyTasks.Remove(t);
                    }
                    // remove from taskList if it is present there
                    tasksToRemove.Clear();
                    foreach (WorldDisplayTask t in taskList)
                    {
                        if (t.chunkX == task.chunkX && t.chunkZ == task.chunkZ && t.type == WorldDisplayTaskType.DISPLAY)
                        {
                            tasksToRemove.Add(t);
                        }
                    }

                    foreach (WorldDisplayTask t in tasksToRemove)
                    {
                        taskList.Remove(t);
                    }
                    
                    // plan unload from the engine if it is currently displayed
                    if (displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                    {
                        columnsToHide.Add(((task.chunkX, task.chunkZ)));
                    }
                    break;
            }
        }
    }

    /**
     * Unloads all chunk columns that were planned to be removed.
     * Expects outside invocation by the main Godot thread.
     */
    public void ProcessPlannedMeshUnloads()
    {
        lock (taskAccessLock)
        {
            // process all columns to be removed
            foreach (var column in columnsToHide)
            {
                if (displayedColumns.ContainsKey(column))
                {
                    // remove its geometry and collision instance from the engine
                    DisplayedColumn displayedColumn = displayedColumns[column];
                    for (int y = 0; y < chunkCountY; y++)
                    {
                        StaticBody3D collision = displayedColumn.chunkCollisions[y];
                        if (collision != null)
                        {
                            collision.QueueFree();
                            displayedColumn.chunkCollisions[y] = null;
                        }
                            
                        MeshInstance3D instance = displayedColumn.chunkInstances[y];
                        if (instance != null)
                        {
                            instance.QueueFree();
                            displayedColumn.chunkInstances[y] = null;
                        }
                    }
                    displayedColumns.Remove(column);
                }
            }
            columnsToHide.Clear(); // reset
        }
    }

    /**
     * TODO update description
     * Processes single scheduled display / update task.
     * Creates engine mesh instances and places the into the godot scene.
     * All mesh instances are placed to shifted engine scene positions defined by origin shift offset.
     * DISPLAY tasks include LOD transitions - unload of old LOD geometry and display of new one.
     * UPDATE_EDIT tasks only replace single chunk of the column with the updated geometry.
     * For columns currently displayed at LOD0 is also placed a collision body build from their LOD1 version. 
     */
    public bool ProcessDisplayUpdateTask()
    {
        // try to obtain first pre-processed task
        PreProcessedChunkColumn preProcessedColumn = null;
        lock (taskAccessLock) // maybe not needed
        {
            if (preProcessedColumns.Count > 0)
            {
                preProcessedColumn = preProcessedColumns[0];
                preProcessedColumns.RemoveAt(0);
            }
        }

        if (preProcessedColumn == null)
        {
            return false;
        }

        MeshInstance3D instance;
        Vector3Int worldPositionOffset;
        WorldDisplayTask task = preProcessedColumn.task;

        switch (task.type)
        {
            // displaying a new column or updating an existing one to different LOD
            case WorldDisplayTaskType.DISPLAY:
            {
                // firstly check and queue-free existing if needed
                // displayed column was prepared during pre-process
                DisplayedColumn displayedColumn;
                if (displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                {
                    displayedColumn = displayedColumns[(task.chunkX, task.chunkZ)];
                }
                else
                {
                    // might have been already discarded
                    return true;
                }
                
                // existing column might already be at correct lod level, should not really happen
                if (displayedColumn.currentLod == task.lodLevel)
                {
                    return true;
                }
                
                displayedColumn.currentLod = task.lodLevel;
                
                // queue-free existing
                for (int y = 0; y < chunkCountY; y++)
                {
                    // unload old chunk collision if updating
                    if (displayedColumn.chunkCollisions[y] != null)
                    {
                        displayedColumn.chunkCollisions[y].QueueFree();
                        displayedColumn.chunkCollisions[y] = null;
                    }

                    // cleanup old geometry instance if updating
                    if (displayedColumn.chunkInstances[y] != null)
                    {
                        displayedColumn.chunkInstances[y].QueueFree();
                        displayedColumn.chunkInstances[y] = null;
                    }
                }

                // process all its chunks
                for (int y = 0; y < chunkCountY; y++)
                {
                    if (preProcessedColumn.chunks[y] == null)
                    {
                        continue; // todo check correct
                    }

                    ArrayMesh arrayMesh = preProcessedColumn.chunks[y].arrayMesh;
                    if (arrayMesh == null)
                    {
                        continue;
                    }

                    instance = new MeshInstance3D();
                    instance.Mesh = arrayMesh;
                    // assign it and apply origin shift offset
                    displayedColumn.chunkInstances[y] = instance;
                    displayedColumn.realPositions[y] = preProcessedColumn.chunks[y].worldPosition;
                    worldPositionOffset = preProcessedColumn.chunks[y].worldPosition + originShiftOffsetXZ;
                    instance.Position = worldPositionOffset.ToGodotVector3();
                    voxelWorld.AddChild(instance);
                    
                    // add collision if current LOD is LOD0 (use LOD2 for collision)
                    if (task.lodLevel == LodLevel.LOD0)
                    {
                        StaticBody3D staticBodyNode = collisionScene.Instantiate<StaticBody3D>();
                        ConcavePolygonShape3D shape = new ConcavePolygonShape3D();
                        shape.SetFaces(preProcessedColumn.chunks[y].collisionGeometry);
                        staticBodyNode.GetNode<CollisionShape3D>("CollisionShape").SetShape(shape);
                        instance.AddChild(staticBodyNode);
                    }
                }
                
                break;
            }
            
            // only updating a single chunk of a column to its new geometry
            case WorldDisplayTaskType.UPDATE_EDIT:
            {
                // firstly check and queue-free existing if needed
                // displayed column should exist
                DisplayedColumn displayedColumn;
                if (displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                {
                    displayedColumn = displayedColumns[(task.chunkX, task.chunkZ)];
                }
                else
                {
                    // might have been already discarded, or error
                    return true;
                }
                
                // queue-free existing
                // unload old chunk collision if updating
                if (displayedColumn.chunkCollisions[task.chunkY] != null)
                {
                    displayedColumn.chunkCollisions[task.chunkY].QueueFree();
                    displayedColumn.chunkCollisions[task.chunkY] = null;
                }

                // cleanup old geometry instance if updating
                if (displayedColumn.chunkInstances[task.chunkY] != null)
                {
                    displayedColumn.chunkInstances[task.chunkY].QueueFree();
                    displayedColumn.chunkInstances[task.chunkY] = null;
                }
                
                // process a single chunk
                // pre-processed column has only a single chunk prepared, stored on 0-th index
                if (preProcessedColumn.chunks[0] == null)
                {
                    return true;
                }
                
                ArrayMesh arrayMesh = preProcessedColumn.chunks[0].arrayMesh;
                if (arrayMesh == null)
                {
                    return true;
                }
                
                instance = new MeshInstance3D();
                instance.Mesh = arrayMesh;
                // assign it and apply origin shift offset
                displayedColumn.chunkInstances[task.chunkY] = instance;
                displayedColumn.realPositions[task.chunkY] = preProcessedColumn.chunks[0].worldPosition;
                worldPositionOffset = preProcessedColumn.chunks[0].worldPosition + originShiftOffsetXZ;
                instance.Position = worldPositionOffset.ToGodotVector3();
                voxelWorld.AddChild(instance);
                
                // add collision if current LOD is LOD0 (use LOD2 for collision)
                if (displayedColumn.currentLod == LodLevel.LOD0)
                {
                    StaticBody3D staticBodyNode = collisionScene.Instantiate<StaticBody3D>();
                    ConcavePolygonShape3D shape = new ConcavePolygonShape3D();
                    shape.SetFaces(preProcessedColumn.chunks[0].collisionGeometry);
                    staticBodyNode.GetNode<CollisionShape3D>("CollisionShape").SetShape(shape);
                    instance.AddChild(staticBodyNode);
                }
                
                break;
            }
        }

        return true;
    }
    
    /**
     * Sets new origin shift offset. This offset will be used with all future applications.
     * All currently displayed columns are immediately repositioned.
     */
    public void ApplyOriginShift(Vector3Int newOriginShiftOffsetXZ)
    {
        originShiftOffsetXZ = newOriginShiftOffsetXZ;

        foreach (DisplayedColumn displayedColumn in displayedColumns.Values)
        {
            for (int y = 0; y < chunkCountY; y++)
            {
                // reposition its mesh instance, not moving collision bodies, as these are attached to the instances
                MeshInstance3D instance = displayedColumn.chunkInstances[y];
                if (instance != null)
                {
                    Vector3Int worldPosition = displayedColumn.realPositions[y];
                    Vector3Int worldPositionOffset = worldPosition + originShiftOffsetXZ;
                    instance.Position = worldPositionOffset.ToGodotVector3();
                }
            }
        }
    }
    
    /**
     * Unloads all currently displayed chunk column meshes and their collision bodies.
     */
    public void Dispose()
    {
        stopThread = true;
        processingThread.Join();
        processingThread = null;
        
        foreach (DisplayedColumn displayedColumn in displayedColumns.Values)
        {
            // remove its chunk collisions
            foreach (StaticBody3D chunkCollision in displayedColumn.chunkCollisions)
            {
                if (chunkCollision != null)
                {
                    chunkCollision.QueueFree();
                }
            }
            // remove its chunk geometry mesh instances
            foreach (MeshInstance3D chunkInstance in displayedColumn.chunkInstances)
            {
                if (chunkInstance != null)
                {
                    chunkInstance.QueueFree();
                }
            }
        }
    }
}
