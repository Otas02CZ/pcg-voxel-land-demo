// FILE: VoxelEditService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Allows asynchronous voxel editing of the game world.

using System;
using System.Threading.Tasks;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Allows asynchronous voxel editing of the world voxel structure via raycasts.
 */
public class VoxelEditService
{
    private readonly VoxelStorage voxelStorage;
    private readonly StorageService storageService;
    private readonly WorldDisplayService worldDisplayService;

    private readonly byte voxelsPerMeter;
    private readonly byte chunkCountY;
    private readonly byte chunkVoxelSize;
    // signals
    private Action<int, uint, int> onChunkModified;
    private Action onEditFailed;
    
    public VoxelEditService(VoxelStorage voxelStorage, StorageService storageService, WorldDisplayService worldDisplayService, byte voxelsPerMeter, byte chunkCountY, byte chunkVoxelSize)
    {
        this.voxelStorage = voxelStorage;
        this.storageService = storageService;
        this.worldDisplayService = worldDisplayService;
        this.voxelsPerMeter = voxelsPerMeter;
        this.chunkCountY = chunkCountY;
        this.chunkVoxelSize = chunkVoxelSize;
    }
    
    /**
     * Asynchronous startup of a voxel editing raycast.
     * Failure invokes onEditFailed signal to re-enable editing in root.
     */
    public void RaycastVoxelEditAsync(Vector3Double origin, Vector3 direction, float maxDistance, VoxelType voxelType, byte voxelSize, bool placeMode)
    {
        Task.Run(() =>
        {
            bool success = RaycastVoxelEdit(origin, direction, maxDistance, voxelType, voxelSize, placeMode);
            if (!success)
                onEditFailed?.Invoke();
        });
    }
    
    /**
     * First step of runtime player voxel editing.
     * Uses raycasts in the voxel structure to find the area to edit.
     * Ray starts from given origin and traverses the voxel structure at supplied voxel level in given direction.
     * If maximum distance threshold is hit without hitting any non-air voxel, raycast ends with no edit.
     * If non-air voxel is hit and placeMode is true, voxel of given size and type is placed at the previous voxel position,
     * otherwise with false placeMode, the hit voxel is replaced with air. In both cases onChunkModified is signaled so that geometry service can remesh it.
     * Raycast also stops when reaching unfinished voxel chunk columns or columns that are not yet displayed.
     *
     * This implementation was inspired by this article: http://gamedev.net/blogs/entry/2265248-voxel-traversal-algorithm-ray-casting/
     * And uses technique RLV introduced in https://www.researchgate.net/publication/2611491_A_Fast_Voxel_Traversal_Algorithm_for_Ray_Tracing
     */
    private bool RaycastVoxelEdit(Vector3Double origin, Vector3 direction, float maxDistance, VoxelType voxelType, byte voxelSize, bool placeMode)
    {
        // voxel size in meters, distance tracking
        float voxelSizeInMeters = (float)voxelSize / voxelsPerMeter;
        
        // starting position in voxel space
        int currentVoxelX = (int)Math.Floor(origin.x * voxelsPerMeter / voxelSize) * voxelSize;
        int currentVoxelY = (int)Math.Floor(origin.y * voxelsPerMeter / voxelSize) * voxelSize;
        int currentVoxelZ = (int)Math.Floor(origin.z * voxelsPerMeter / voxelSize) * voxelSize;
        
        // dda step
        int stepX = (direction.X >= 0f) ? voxelSize : -voxelSize;
        int stepY = (direction.Y >= 0f) ? voxelSize : -voxelSize;
        int stepZ = (direction.Z >= 0f) ? voxelSize : -voxelSize;
        
        // delta in meters for stepping to next voxel boundary in each axis
        double deltaX = (direction.X != 0f) ? MathF.Abs(voxelSizeInMeters / direction.X) : double.MaxValue;
        double deltaY = (direction.Y != 0f) ? MathF.Abs(voxelSizeInMeters / direction.Y) : double.MaxValue;
        double deltaZ = (direction.Z != 0f) ? MathF.Abs(voxelSizeInMeters / direction.Z) : double.MaxValue;
        
        // maximum distances in meters to next voxel boundary in each axis
        double maxX, maxY, maxZ;
        // initialize from the starting point
        if (direction.X == 0f)
            maxX = double.MaxValue;
        else if (direction.X > 0f)
            maxX = ((currentVoxelX + voxelSize) / (double)voxelsPerMeter - origin.x) / direction.X;
        else
            maxX = (currentVoxelX / (double)voxelsPerMeter - origin.x) / direction.X;
        
        if (direction.Y == 0f)
            maxY = double.MaxValue;
        else if (direction.Y > 0f)
            maxY = ((currentVoxelY + voxelSize) / (double)voxelsPerMeter - origin.y) / direction.Y;
        else
            maxY = (currentVoxelY / (double)voxelsPerMeter - origin.y) / direction.Y;
        
        if (direction.Z == 0f)
            maxZ = double.MaxValue;
        else if (direction.Z > 0f)
            maxZ = ((currentVoxelZ + voxelSize) / (double)voxelsPerMeter - origin.z) / direction.Z;
        else
            maxZ = ((currentVoxelZ / (double)voxelsPerMeter - origin.z) / direction.Z);
        
        // previous voxel and column position
        int lastVoxelX = currentVoxelX;
        int lastVoxelY = currentVoxelY;
        int lastVoxelZ = currentVoxelZ;
        int lastColX = 0;
        int lastColZ = 0;
        bool hasPrev = false;
        
        // distance tracking
        double distance = 0f;
        int totalVoxelsY = chunkCountY * chunkVoxelSize;
        
        while (distance <= maxDistance)
        {
            // vertical bounds check
            if (currentVoxelY < 0 || currentVoxelY >= totalVoxelsY)
                return false;
            
            // determine current chunk column
            int colX = (int)Math.Floor((double)currentVoxelX / chunkVoxelSize);
            int colZ = (int)Math.Floor((double)currentVoxelZ / chunkVoxelSize);
            
            // check that column is loaded, generated and displayed
            ChunkColumn column = voxelStorage.GetChunkColumn(colX, colZ);
            if (column == null)
                return false;
            if (!column.IsFullyGenerated())
                return false;
            if (!worldDisplayService.IsColumnDisplayed(colX, colZ))
                return false;
            
            // obtain voxel at current position
            VoxelType current = column.GetVoxel(currentVoxelX, (uint)currentVoxelY, currentVoxelZ, voxelSize);
            
            // hit a non-air voxel
            if (current != VoxelType.AIR)
            {
                if (placeMode) // placing new voxel at previous position
                {
                    if (!hasPrev)
                        return false; // no previous air voxel available

                    // get column to place into
                    ChunkColumn placeColumn = voxelStorage.GetChunkColumn(lastColX, lastColZ);
                    if (placeColumn == null)
                        return false;
                    
                    // set voxel to given type and save voxel operation to drive
                    placeColumn.SetVoxel(lastVoxelX, (uint)lastVoxelY, lastVoxelZ, voxelType, voxelSize);
                    if (!storageService.SaveVoxelOperation(colX, colZ, lastVoxelX, lastVoxelY, lastVoxelZ, voxelSize, voxelType))
                    {
                        GD.PrintErr($"Failed to save voxel operation in column {colX}, {colZ}.");
                    }
                    
                    // simplify modified chunk
                    uint placeChunkY = (uint)(lastVoxelY / chunkVoxelSize);
                    placeColumn.Simplify(placeChunkY);
                    // invoke signal for modified chunk so that geometry service can remesh it
                    onChunkModified?.Invoke(lastColX, placeChunkY, lastColZ);
                    
                    return true;
                }
                else // removing found voxel
                {
                    // set voxel to air
                    column.SetVoxel(currentVoxelX, (uint)currentVoxelY, currentVoxelZ, VoxelType.AIR, voxelSize);
                    if (!storageService.SaveVoxelOperation(colX, colZ, currentVoxelX, currentVoxelY, currentVoxelZ, voxelSize, VoxelType.AIR))
                    {
                        GD.PrintErr($"Failed to save voxel operation in column {colX}, {colZ}.");
                    }
                    // simplify modified chunk
                    uint hitChunkY = (uint)(currentVoxelY / chunkVoxelSize);
                    column.Simplify(hitChunkY);
                    // invoke signal for modified chunk so that geometry service can remesh it
                    onChunkModified?.Invoke(colX, hitChunkY, colZ);
                    
                    return true;
                }
            }
            
            // current voxel is air, continue
            lastVoxelX = currentVoxelX;
            lastVoxelY = currentVoxelY;
            lastVoxelZ = currentVoxelZ;
            lastColX = colX;
            lastColZ = colZ;
            hasPrev = true;
            
            // dda step in voxel space
            // continue with axis of currently shortest distance 
            if (maxX < maxY && maxX < maxZ)
            {
                distance = maxX;
                maxX += deltaX;
                currentVoxelX += stepX;
            }
            else if (maxY < maxZ)
            {
                distance = maxY;
                maxY += deltaY;
                currentVoxelY += stepY;
            }
            else
            {
                distance = maxZ;
                maxZ += deltaZ;
                currentVoxelZ += stepZ;
            }
        }
        
        return false;
    }
    
    /**
     * Subscribe to chunk modified signal.
     */
    public void SubscribeOnChunkModified(Action<int, uint, int> handler)
    {
        onChunkModified += handler;
    }
    
    /**
     * Subscribe to edit failed signal.
     */
    public void SubscribeOnEditFailed(Action handler)
    {
        onEditFailed += handler;
    }
}
