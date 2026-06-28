// FILE: VoxelModel.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains necessities for work with VoxelModels

using System.Collections.Generic;
using System.Numerics;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Simplified array based voxel container for creation of large voxel models.
 * Voxels are mapped to one dimensional array. Supports filling by Bézier curves and spheres.
 */
public class VoxelModel
{
    private const int bezierCurveSteps = 100; // steps along bezier curve
    private const float stepSize = 1f / bezierCurveSteps;
    
    private readonly ushort sizeX;
    private readonly ushort sizeY;
    private readonly ushort sizeZ;
    private readonly VoxelType[] data; // voxel storage in one dimensional array
    // precomputed limits and constants for this model
    private readonly short halfSizeX;
    private readonly short halfSizeZ;
    private readonly short minX;
    private readonly short minY;
    private readonly short minZ;
    private readonly short maxX;
    private readonly short maxY;
    private readonly short maxZ;
    
    public VoxelModel(ushort sizeX, ushort sizeY, ushort sizeZ)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;

        // precompute limits and constants
        halfSizeX = (short)(sizeX / 2);
        halfSizeZ = (short)(sizeZ / 2);
        minX = (short)(-halfSizeX);
        minY = 0;
        minZ = (short)(-halfSizeZ);
        maxX = halfSizeX;
        maxY = (short)sizeY;
        maxZ = halfSizeZ;
        
        data = new VoxelType[sizeX * sizeY * sizeZ];
    }
    
    /**
     * 3D to 1D mapping function
     * 
     * 3D space boundaries:
     * -sizeX/2 to sizeX/2, 0 to sizeY, -sizeZ/2 to sizeZ/2
     */
    private uint MappingFunction(short x, short y, short z)
    {
        return (uint)((x + halfSizeX) + y * sizeX + (z + halfSizeZ) * sizeX * sizeY);
    }
    
    /**
     * Returns voxel at position. Checks bounds.
     */
    public VoxelType GetVoxel(short x, short y, short z)
    {
        if (x < minX || x >= maxX || y < minY || y >= maxY || z < minZ || z >= maxZ)
            return VoxelType.AIR;
        return data[MappingFunction(x, y, z)];
    }
    
    /**
     * Returns voxel at position. Does not check bounds.
     */
    private VoxelType GetVoxelInternal(short x, short y, short z)
    {
        return data[MappingFunction(x, y, z)];
    }
    
    /**
     * Sets voxel at position. Checks bounds.
     */
    public void SetVoxel(short x, short y, short z, VoxelType type)
    {
        if (x < minX || x >= maxX || y < minY || y >= maxY || z < minZ || z >= maxZ)
            return;
        data[MappingFunction(x, y, z)] = type;
    }
    
    /**
     * Returns voxel model as array of VoxelItems. Only non-air voxels are returned.
     */
    public VoxelItem[] GetVoxelItems()
    {
        List<VoxelItem> itemList = [];
        
        for (short x = minX; x < maxX; x++)
        {
            for (short y = minY; y < maxY; y++)
            {
                for (short z = minZ; z < maxZ; z++)
                {
                    VoxelType type = GetVoxelInternal(x, y, z);
                    if (type != VoxelType.AIR)
                    {
                        itemList.Add(new VoxelItem(x, y, z, type));
                    }
                }
            }
        }

        return itemList.ToArray();
    }
    
    /**
     * Fills voxel space with sphere of given radius and voxel type at specified position
     */
    public void FillSphere(Vector3 position, short radius, VoxelType type)
    {
        short centerX = (short)position.X;
        short centerY = (short)position.Y;
        short centerZ = (short)position.Z;
        short radiusSquared = (short)(radius * radius);

        for (short dX = (short)(-radius); dX <= radius; dX++)
        {
            for (short dY = (short)(-radius); dY <= radius; dY++)
            {
                for (short dZ = (short)(-radius); dZ <= radius; dZ++)
                {
                    short distanceSquared = (short)(dX * dX + dY * dY + dZ * dZ);
                    if (distanceSquared <= radiusSquared)
                    {
                        short voxelX = (short)(centerX + dX);
                        short voxelY = (short)(centerY + dY);
                        short voxelZ = (short)(centerZ + dZ);
                        
                        // do not replace already placed non-air voxels
                        if (GetVoxelInternal(voxelX, voxelY, voxelZ) == VoxelType.AIR)
                        {
                            SetVoxel(voxelX, voxelY, voxelZ, type);
                        }
                    }
                }
            }
        }
    }

    /**
     * Fills voxel space by spheres placed along a Bézier curve evaluated with control points supplied in path
     */
    public void FillByBezierCurve(List<Vector3> path, short radius, VoxelType type)
    {
        for (int step = 0; step <= bezierCurveSteps; step++)
        {
            float dist = step * stepSize;
            Vector3 point = EvaluateBezierCurve(path, dist);
    
            FillSphere(point, radius, type);
        }
    }

    /**
     * Evaluates Bézier curve on a path at given distance from start with De Casteljau algorithm.
     */
    private Vector3 EvaluateBezierCurve(List<Vector3> path, float distance) {
        if (path.Count == 1)
            return path[0]; 
        
        List<Vector3> tempPoints = new List<Vector3>(path);

        while (tempPoints.Count > 1)
        {
            List<Vector3> newPoints = [];
    
            for (int i = 0; i < tempPoints.Count - 1; i++)
            {
                Vector3 interpolated = Vector3.Lerp(tempPoints[i], tempPoints[i + 1], distance);
                newPoints.Add(interpolated);
            }
    
            tempPoints = newPoints;
        }

        return tempPoints[0];
    }
}

/**
 * VoxelItem representing a single placement of voxel type at given position.
 */
public struct VoxelItem(short x, short y, short z, VoxelType type)
{
    public short X { get; set; } = x;
    public short Y { get; set; } = y;
    public short Z { get; set; } = z;
    public VoxelType Type { get; } = type;
}
