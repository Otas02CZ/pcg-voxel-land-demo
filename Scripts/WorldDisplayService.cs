// FILE: WorldDisplayService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains Service class that displays and hides chunk geometry in the world in the engine. 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

/**
 * Structure holding surface data of a chunk, that is required to assemble ArrayMesh later on.
 */
public class SurfaceData(Godot.Collections.Array geometry, Mesh.ArrayFormat format, Material material)
{
    public readonly Godot.Collections.Array geometry = geometry;
    public readonly Mesh.ArrayFormat format = format;
    public readonly Material material = material;
}

/**
 * Stores pre-processed data for a single chunk. These data are then quickly applied on the main thread.
 */
public class PreProcessedChunk(SurfaceData[] surfaceData, Vector3[] collisionGeometry, Vector3Int worldPosition)
{
    public readonly SurfaceData[] surfaceData = surfaceData;
    public readonly Vector3[] collisionGeometry = collisionGeometry;
    public readonly Vector3Int worldPosition = worldPosition;
}

/**
 * Stores pre-processed data of a chunk column. These data are then quickly applied on the main thread
 */
public class PreProcessedChunkColumn(PreProcessedChunk[] chunks, WorldDisplayTask task)
{
    public readonly PreProcessedChunk[] chunks = chunks;
    public readonly WorldDisplayTask task = task;
}

/**
 * Represents data associated with column, which chunks are currently being applied.
 */
public class CurrentWorkingColumn
{
    public PreProcessedChunkColumn data;
    public uint nextChunkY;
}

/**
 * Chunk column geometry display, update and unload service for displaying world voxel geometry in the Godot Engine.
 * Includes support for LOD changes.
 * Controlled by a task system. Display (includes LOD changes) and changes due to user voxel editing are run on a single task basis.
 * Complete unloading of columns is done in batches when all unload operations are done at once.
 * Its work is split into two parts. Firstly the required tasks are pre-processed asynchronously on the main thread (both column display and chunk edit updates).
 * This includes geometry and collision unpacking and preparation, etc.
 * Secondly, these pre-processed tasks are applied to the engine from the main thread. The application is divided to chunks
 * and their processing is distributed between invocations of ProcessDisplayUpdateTask function. Reducing the load on the main thread and stuttering.
 * Supports repositioning of column geometry to engine world center to eliminate floating point errors during rendering and physics.
 */
public class WorldDisplayService
{
    private readonly ConcurrentDictionary<(int, int), DisplayedColumn> displayedColumns; // currently displayed columns
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
    private CurrentWorkingColumn workColumn = null;
    
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
        displayedColumns = new ConcurrentDictionary<(int, int), DisplayedColumn>();
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
     * Pre-processes supplied display/update tasks asynchronously on the engine main thread.
     * Pre-processing includes geometry and collision unpacking and preparation, ArrayMesh assembly, etc.
     * Fills a buffer of pre-processed tasks.
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
                    displayedColumns[(task.chunkX, task.chunkZ)] = displayedColumn;
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
                        SurfaceData[] surfaceData = CreateChunkSurfaceData(chunkGeometry, voxelVariance);
                        
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
                        
                        preProcessedChunks[y] = new PreProcessedChunk(surfaceData, collisionGeometryData, chunkGeometry.worldPosition);
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

                    ChunkGeometry chunkGeometry = geometry.chunks[task.chunkY];
                    bool voxelVariance = lod == LodLevel.LOD0; // only LOD0 has variance shader for non water blocks
                    SurfaceData[] surfaceData = CreateChunkSurfaceData(chunkGeometry, voxelVariance);

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
                    preProcessedChunks[0] = new PreProcessedChunk(surfaceData, collisionGeometryData, chunkGeometry.worldPosition);
                    
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
     * Creates SurfaceData structure with information required to assemble ArrayMesh of given ChunkGeometry later.
     */
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private SurfaceData[] CreateChunkSurfaceData(ChunkGeometry geometryData, bool enableVoxelVariance)
    {
        try
        {
            List<SurfaceData> surfaceDataList = new List<SurfaceData>();

            // surface 0 - solid blocks
            if (geometryData.hasGeometry && geometryData.vertices?.Length > 0)
            {
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

                Material surfaceMaterial = enableVoxelVariance ? blockMaterialVariance : blockMaterialBasic;
                SurfaceData solidSurface = new SurfaceData(solidArrays, arrayFormat, surfaceMaterial);
                surfaceDataList.Add(solidSurface);
            }

            // surface 1 - water
            if (geometryData.hasWaterGeometry && geometryData.waterVertices?.Length > 0)
            {
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

                SurfaceData waterSurface = new SurfaceData(waterArrays, arrayFormat, waterMaterial);
                surfaceDataList.Add(waterSurface);
            }

            if (surfaceDataList.Count == 0)
            {
                return null;
            }

            SurfaceData[] surfaceData = surfaceDataList.ToArray();
            return surfaceData;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to create surface data for chunk {geometryData.chunkX},{geometryData.chunkY},{geometryData.chunkZ}: {ex.Message}");
            return null;
        }
    }
    
    /**
     * Creates ArrayMesh with assembled surfaces of given ChunkGeometry.
     * NOTE: After hours of debugging random crashes with segmentation faults, I found out that the AddSurfaceFromArrays function
     * can sometimes internally cause seg fault while performing dictionary operations (stacktrace get_key_list, last entry Variant::reference)
     * when lods dictionary is not supplied. Interestingly this seems to be fixed when an empty lod dictionary is supplied.
     * TODO: Might need to investigate this weird behavior further, maybe some side effect? Esp. if the issues happen again.
     */
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private ArrayMesh CreateChunkArrayMesh(SurfaceData[] surfaceData)
    {
        try
        {
            ArrayMesh arrayMesh = new();

            if (surfaceData == null || surfaceData.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < surfaceData.Length; i++)
            {
                SurfaceData surface = surfaceData[i];
                // assigning an empty Dictionary to lods seems to fix random crashes (seg faults) during calling this internal function
                arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface.geometry, lods: new Godot.Collections.Dictionary(), flags: surface.format);
                arrayMesh.SurfaceSetMaterial(i, surface.material);
            }
            
            return arrayMesh;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to create combined mesh: {ex.Message}");
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
                    displayedColumns.TryRemove((column.chunkX, column.chunkZ), out _);
                }
            }
            columnsToHide.Clear(); // reset
        }
    }

    /**
     * Finalize (pre-processed) display chunk column updates,
     * uses ApplyChunkUpdate to create and place the instances to the engine.
     * UPDATE_EDIT tasks are applied immediately.
     * DISPLAY task application is divided into chunks and their processing is distributed between
     * the invocations of this function (workColumn) to minimize stuttering.
     */
    public void ProcessDisplayUpdateTask()
    {
        // take new column to work on if last was finished
        if (workColumn == null)
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
                return;
            }
            
            // UPDATE_EDIT task should be processed right away, no distributed work
            if (preProcessedColumn.task.type == WorldDisplayTaskType.UPDATE_EDIT)
            {
                ApplyChunkUpdate(preProcessedColumn.task, preProcessedColumn.chunks[0], preProcessedColumn.task.chunkY);
                return;
            }
            // otherwise DISPLAY task is present - load up new column for distributed work
            workColumn = new CurrentWorkingColumn();
            workColumn.data = preProcessedColumn;
            workColumn.nextChunkY = 0;
        }
        // process single chunk of currently active working column
        PreProcessedChunkColumn processedColumn = workColumn.data;
        uint y = workColumn.nextChunkY;
        
        workColumn.nextChunkY++;
        if (workColumn.nextChunkY == chunkCountY)
        {
            workColumn = null;
        }
        
        ApplyChunkUpdate(processedColumn.task, processedColumn.chunks[y], y);
    }

    /*
     * Applies a single chunk update (for both DISPLAY and UPDATE_EDIT tasks).
     * Creates engine mesh instances and places them into the godot scene.
     * All mesh instances are placed to shifted engine scene positions defined by origin shift offset.
     * DISPLAY tasks include LOD transitions - unload of old LOD geometry and display of new one.
     * UPDATE_EDIT tasks only replace single chunk of the column with the updated geometry.
     * For columns currently displayed at LOD0 is also placed a collision body build from their LOD2 version.
     */
    private void ApplyChunkUpdate(WorldDisplayTask task, PreProcessedChunk chunk, uint chunkY)
    {
        // displayed column should exist
        DisplayedColumn displayedColumn;
        if (displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
        {
            displayedColumn = displayedColumns[(task.chunkX, task.chunkZ)];
        }
        else
        {
            // might have been already discarded, or error
            return;
        }
        
        if (task.type == WorldDisplayTaskType.DISPLAY)
        {
            displayedColumn.currentLod = task.lodLevel;
        }
        
        // queue-free existing
        // unload old chunk collision if updating
        if (displayedColumn.chunkCollisions[chunkY] != null)
        {
            displayedColumn.chunkCollisions[chunkY].QueueFree();
            displayedColumn.chunkCollisions[chunkY] = null;
        }

        // cleanup old geometry instance if updating
        if (displayedColumn.chunkInstances[chunkY] != null)
        {
            displayedColumn.chunkInstances[chunkY].QueueFree();
            displayedColumn.chunkInstances[chunkY] = null;
        }
        
        if (chunk == null)
        {
            return;
        }
        
        ArrayMesh arrayMesh = CreateChunkArrayMesh(chunk.surfaceData);
        if (arrayMesh == null)
        {
            return;
        }
        // add mesh instance
        MeshInstance3D instance = new MeshInstance3D();
        instance.Mesh = arrayMesh;
        // assign it and apply origin shift offset
        displayedColumn.chunkInstances[chunkY] = instance;
        displayedColumn.realPositions[chunkY] = chunk.worldPosition;
        Vector3Int worldPositionOffset = chunk.worldPosition + originShiftOffsetXZ;
        instance.Position = worldPositionOffset.ToGodotVector3();
        voxelWorld.AddChild(instance);
        
        // add collision if current LOD is LOD0 (use LOD2 for collision)
        if (displayedColumn.currentLod == LodLevel.LOD0)
        {
            StaticBody3D staticBodyNode = collisionScene.Instantiate<StaticBody3D>();
            ConcavePolygonShape3D shape = new ConcavePolygonShape3D();
            shape.SetFaces(chunk.collisionGeometry);
            staticBodyNode.GetNode<CollisionShape3D>("CollisionShape").SetShape(shape);
            instance.AddChild(staticBodyNode);
        }
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
        // thread cleanup
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
