// FILE: WorldDisplayService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains Service class that displays and hides chunk geometry in the world in the engine. 

using System;
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
 * Chunk column geometry display, update and unload service for displaying world voxel geometry in the Godot Engine.
 * Includes support for LOD changes.
 * Controlled by a task system. Display (includes LOD changes) and changes due to user voxel editing are run on a single task basis.
 * Complete unloading of columns is done in batches when all unload operations are done at once.
 * Does not process tasks on its own via threading as other services do. But exposes the jobs to be done from outside by main thread
 * from root so that mesh changes in Godot scene happen correctly on the main thread.
 * Supports repositioning of column geometry to engine world center to eliminate floating point errors during rendering and physics.
 * TODO: try rework with async processing
 */
public class WorldDisplayService
{
    private readonly Dictionary<(int, int), DisplayedColumn> displayedColumns; // currently displayed columns
    private readonly List<WorldDisplayTask> taskList; // remaining tasks, that can be processed in next invocations of ProcessDisplayUpdateTask
    private readonly List<WorldDisplayTask> notReadyTasks; // tasks scheduled by root, but waiting for geometry or lower steps of generation
    private readonly Lock taskAccessLock;

    private readonly List<(int chunkX, int chunkZ)> columnsToHide; // columns to unload in next batched removal
    // dependencies
    private readonly GeometryGeneratorService geometryGeneratorService;
    private readonly ShaderMaterial waterMaterial;
    private readonly ShaderMaterial blockMaterialVariance;
    private readonly StandardMaterial3D blockMaterialBasic;
    private readonly Node3D voxelWorld;
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
     * Creates Godot Engine MeshInstance3D with block and water surfaces from geometry of supplied chunk.
     * Optimizations must be disabled, otherwise I had issues with csharp - godot interconnect bridge, that resulted in broken and out of order array operations on internal
     * engine data structures.
     */
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private MeshInstance3D CreateChunkMeshInstance(ChunkMesh meshData, bool enableVoxelVariance)
    {
        try
        {
            ArrayMesh arrayMesh = new();
            bool hasAnySurface = false;

            // surface 0 - solid blocks
            if (meshData.hasGeometry && meshData.vertices?.Length > 0)
            {
                hasAnySurface = true;

                // extend compacted normals and uv arrays
                int baseIndex;
                Vector3[] extendedNormals = new Vector3[meshData.vertices.Length];
                for (int i = 0; i < meshData.normals.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedNormals[baseIndex] = meshData.normals[i];
                    extendedNormals[baseIndex + 1] = meshData.normals[i];
                    extendedNormals[baseIndex + 2] = meshData.normals[i];
                    extendedNormals[baseIndex + 3] = meshData.normals[i];
                }

                Vector2[] extendedUVs = new Vector2[meshData.vertices.Length];
                for (int i = 0; i < meshData.uvs.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedUVs[baseIndex] = meshData.uvs[i];
                    extendedUVs[baseIndex + 1] = meshData.uvs[i];
                    extendedUVs[baseIndex + 2] = meshData.uvs[i];
                    extendedUVs[baseIndex + 3] = meshData.uvs[i];
                }
                
                // build engine expected array structure
                Godot.Collections.Array solidArrays = new();
                solidArrays.Resize((int)Mesh.ArrayType.Max);
                solidArrays[(int)Mesh.ArrayType.Vertex] = meshData.vertices;
                solidArrays[(int)Mesh.ArrayType.Normal] = extendedNormals;
                solidArrays[(int)Mesh.ArrayType.TexUV] = extendedUVs;
                solidArrays[(int)Mesh.ArrayType.Index] = meshData.indices;

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
            if (meshData.hasWaterGeometry && meshData.waterVertices?.Length > 0)
            {
                hasAnySurface = true;

                // extend compacted array of normals
                int baseIndex;
                Vector3[] extendedNormals = new Vector3[meshData.waterVertices.Length];
                for (int i = 0; i < meshData.waterNormals.Length; i++)
                {
                    baseIndex = i * 4;
                    extendedNormals[baseIndex] = meshData.waterNormals[i];
                    extendedNormals[baseIndex + 1] = meshData.waterNormals[i];
                    extendedNormals[baseIndex + 2] = meshData.waterNormals[i];
                    extendedNormals[baseIndex + 3] = meshData.waterNormals[i];
                }

                // build array for engine
                Godot.Collections.Array waterArrays = new();
                waterArrays.Resize((int)Mesh.ArrayType.Max);
                waterArrays[(int)Mesh.ArrayType.Vertex] = meshData.waterVertices;
                waterArrays[(int)Mesh.ArrayType.Normal] = extendedNormals;
                waterArrays[(int)Mesh.ArrayType.Index] = meshData.waterIndices;

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

            // create its MeshInstance3D
            MeshInstance3D meshInstance = new MeshInstance3D();
            meshInstance.Mesh = arrayMesh;
            return meshInstance;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to create combined mesh for chunk {meshData.chunkX},{meshData.chunkY},{meshData.chunkZ}: {ex.Message}");
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
        lock (taskAccessLock)
        {
            switch (task.type)
            {
                case WorldDisplayTaskType.DISPLAY:
                    // if column is ready add it to tasks that can be processed, otherwise it must wait
                    ChunkColumnGeometry columnGeometry = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                    if (columnGeometry == null)
                    {
                        notReadyTasks.Add(task);
                    }
                    else
                    {
                        taskList.Add(task);
                    }
                    break;
                case WorldDisplayTaskType.HIDE:
                    // remove from notReadyTasks if it is present
                    List<WorldDisplayTask> tasksToRemove = [];
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
     * Processes single scheduled display / update task.
     * Creates engine mesh instances and places the into the godot scene.
     * All mesh instances are placed to shifted engine scene positions defined by origin shift offset.
     * DISPLAY tasks include LOD transitions - unload of old LOD geometry and display of new one.
     * UPDATE_EDIT tasks only replace single chunk of the column with the updated geometry.
     * For columns currently displayed at LOD0 is also placed a collision body build from their LOD1 version. 
     */
    public void ProcessDisplayUpdateTask()
    {
        // try to obtain the first task
        WorldDisplayTask task = null;
        lock (taskAccessLock)
        {
            if (taskList.Count > 0)
            {
                task = taskList[0];
                taskList.RemoveAt(0);
            }
        }

        if (task == null)
        {
            return; // exit if none
        }

        MeshInstance3D instance;
        MeshInstance3D instanceForCollision;
        Vector3Int worldPositionOffset;

        switch (task.type)
        {
            // displaying a new column or updating an existing one to different LOD
            case WorldDisplayTaskType.DISPLAY:
                // obtain its geometry from geometry service
                ChunkColumnGeometry columnGeometry = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                if (columnGeometry == null)
                {
                    GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null geometry.");
                    return;
                }

                // find out whether the column is currently displayed
                DisplayedColumn displayedColumn;
                if (displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                {
                    displayedColumn = displayedColumns[(task.chunkX, task.chunkZ)]; // it is, lod update will proceed
                }
                else
                {
                    // new column is being displayed
                    displayedColumn = new DisplayedColumn(task.chunkX, task.chunkZ, LodLevel.UNLOADED, chunkCountY);
                    displayedColumns.Add((task.chunkX, task.chunkZ), displayedColumn);
                }
                
                // existing column might already be at correct lod level, should not really happen
                if (displayedColumn.currentLod != task.lodLevel)
                {
                    displayedColumn.currentLod = task.lodLevel;
                    // process all its chunks
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
                        
                        if (!columnGeometry.chunks[y].hasGeometry)
                        {
                            continue; // might not have any geometry to display
                        }
                        
                        ChunkMesh chunkMesh = columnGeometry.chunks[y].lods[(byte)task.lodLevel];
                        bool enableVoxelVariance = task.lodLevel == LodLevel.LOD0; // only LOD0 has variance shader for non water blocks
                        instance = CreateChunkMeshInstance(chunkMesh, enableVoxelVariance); // generate engine MeshInstance3D with block and water surface
                        if (instance == null)
                        {
                            continue;
                        }
                        // assign it and apply origin shift offset
                        displayedColumn.chunkInstances[y] = instance;
                        displayedColumn.realPositions[y] = chunkMesh.worldPosition;
                        worldPositionOffset = chunkMesh.worldPosition + originShiftOffsetXZ;
                        instance.Position = worldPositionOffset.ToGodotVector3();
                        voxelWorld.AddChild(instance);
                            
                        // add collision if current LOD is LOD0 (use LOD1 for collision)
                        if (task.lodLevel == LodLevel.LOD0)
                        {
                            chunkMesh = columnGeometry.chunks[y].lods[(byte)LodLevel.LOD1];
                            instanceForCollision = CreateChunkMeshInstance(chunkMesh, false);
                            if (instanceForCollision == null)
                            {
                                continue;
                            }
                            // engine generates collision from node geometry
                            instanceForCollision.CreateTrimeshCollision();
                            StaticBody3D collision = instanceForCollision.GetChild<StaticBody3D>(0);
                            instanceForCollision.RemoveChild(collision);
                            displayedColumn.chunkCollisions[y] = collision;
                            instance.AddChild(collision);
                            instanceForCollision.QueueFree();
                        }
                    }
                }
                break;
            
            // only updating a single chunk of a column to its new geometry
            case WorldDisplayTaskType.UPDATE_EDIT:
                // check whether it was not already unloaded
                if (!displayedColumns.ContainsKey((task.chunkX, task.chunkZ)))
                {
                    return;
                }
                // obtain the column
                DisplayedColumn displayedColumnToUpdate = displayedColumns[(task.chunkX, task.chunkZ)];
                if (displayedColumnToUpdate.currentLod == LodLevel.UNLOADED)
                {
                    return;
                }
                // obtain the column geometry, will only use a single chunk though
                ChunkColumnGeometry columnGeometryToUpdate = geometryGeneratorService.GetColumn(task.chunkX, task.chunkZ);
                if (columnGeometryToUpdate == null)
                {
                    GD.PrintErr($"Column {task.chunkX}, {task.chunkZ} has null geometry.");
                    return;
                }

                int yIndex = (int)task.chunkY;
                
                // unload old geometry instance
                if (displayedColumnToUpdate.chunkInstances[yIndex] != null)
                {
                    displayedColumnToUpdate.chunkInstances[yIndex].QueueFree();
                    displayedColumnToUpdate.chunkInstances[yIndex] = null;
                }
                    
                // unload old chunk collision
                if (displayedColumnToUpdate.chunkCollisions[yIndex] != null)
                {
                    displayedColumnToUpdate.chunkCollisions[yIndex].QueueFree();
                    displayedColumnToUpdate.chunkCollisions[yIndex] = null;
                }
                
                // create the mesh instance with both surfaces
                ChunkMesh chunkMeshToUpdate = columnGeometryToUpdate.chunks[yIndex].lods[(byte)displayedColumnToUpdate.currentLod];
                bool enableVoxelVarianceForUpdate = displayedColumnToUpdate.currentLod == LodLevel.LOD0;
                instance = CreateChunkMeshInstance(chunkMeshToUpdate, enableVoxelVarianceForUpdate);
                if (instance == null)
                {
                    return;
                }
                
                // assign it and apply origin shift offset
                displayedColumnToUpdate.chunkInstances[yIndex] = instance;
                displayedColumnToUpdate.realPositions[yIndex] = chunkMeshToUpdate.worldPosition;
                worldPositionOffset = chunkMeshToUpdate.worldPosition + originShiftOffsetXZ;
                instance.Position = worldPositionOffset.ToGodotVector3();
                voxelWorld.AddChild(instance);
                            
                // add collision if current LOD is LOD0 (use LOD1 for collision)
                if (displayedColumnToUpdate.currentLod == LodLevel.LOD0)
                {
                    chunkMeshToUpdate = columnGeometryToUpdate.chunks[yIndex].lods[(byte)LodLevel.LOD1];
                    instanceForCollision = CreateChunkMeshInstance(chunkMeshToUpdate, false);
                    if (instanceForCollision == null)
                    {
                        return;
                    }
                    // engine generates collision from node geometry
                    instanceForCollision.CreateTrimeshCollision();
                    StaticBody3D collision = instanceForCollision.GetChild<StaticBody3D>(0);
                    instanceForCollision.RemoveChild(collision);
                    displayedColumnToUpdate.chunkCollisions[yIndex] = collision;
                    instance.AddChild(collision);
                    instanceForCollision.QueueFree();
                }
                break;
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
