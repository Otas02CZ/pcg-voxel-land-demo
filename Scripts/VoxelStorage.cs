// FILE: VoxelStorage.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Voxel storage of the world voxel data. Uses dictionary of active chunk columns that are build from SVO structures.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Supported voxel types.
 * XXX_WATER voxels are specific variants of plant voxels placed into water, these are changed to WATER
 * when geometry of higher LODs is generated to fix empty spots / broken water.
 */
public enum VoxelType : byte
{
    AIR,
    GROUND_GRASS,
    GROUND_GRASS_DARK,
    GROUND_GRASS_DARKER,
    GROUND_GRASS_BROWN,
    GROUND_GRASS_BROWN_DARK,
    GROUND_GRASS_BROWN_YELLOW,
    DIRT,
    WOOD,
    PLANT,
    PLANT_DARK,
    DECIDUOUS_LEAVES,
    CONIFEROUS_LEAVES,
    SAND,
    WATER,
    STONE,
    STONE_DARK,
    STONE_DARKER,
    DRIPSTONE,
    DRIPSTONE_DARK,
    STALACTITE,
    SNOW,
    RED,
    BLUE,
    YELLOW,
    WHITE,
    BROWN,
    // water enclosed plant voxel types
    PLANT_WATER,
    PLANT_DARK_WATER,
    YELLOW_WATER,
    BROWN_WATER,
    RED_WATER,
    BLUE_WATER,
    WHITE_WATER,
}

/**
 * SVO voxel storage.
 * Dynamic dictionary based management of active chunk columns that are build of a set of vertically stacked symmetrical
 * chunks, where each represents a root SVO node.
 * Chunks use own chunk oriented world coordinates, whereas voxels use world coordinates.
 * Chunk columns are interconnected via neighbor links for faster access without the need to query voxel storage.
 */
public class VoxelStorage
{
    // active chunk columns
    private readonly ConcurrentDictionary<(int, int), ChunkColumn> chunkColumns;

    public byte chunkSizeInMeters { get; }
    public byte voxelsPerMeter { get; }
    public byte chunkCountY { get; }
    public byte chunkVoxelSize { get; }

    private readonly byte chunkVoxelSizeMask; // for fast modulo operation
    private readonly byte chunkVoxelSizeShift; // for fast division operation

    public VoxelStorage(byte chunkSizeInMeters, byte voxelsPerMeter, byte chunkCountY)
    {
        this.chunkSizeInMeters = chunkSizeInMeters;
        this.voxelsPerMeter = voxelsPerMeter;
        this.chunkCountY = chunkCountY;
        chunkVoxelSize = (byte)(chunkSizeInMeters * voxelsPerMeter);
        
        // configure mask and shift
        chunkVoxelSizeMask = (byte)(chunkVoxelSize - 1);
        chunkVoxelSizeShift = (byte)System.Numerics.BitOperations.TrailingZeroCount(chunkVoxelSize);

        chunkColumns = new ConcurrentDictionary<(int, int), ChunkColumn>();
    }

    /**
     * Return chunk column with given X, Z coordinate if it exists or null.
     */
    public ChunkColumn GetChunkColumn(int chunkX, int chunkZ)
    {
        if (chunkColumns.TryGetValue((chunkX, chunkZ), out ChunkColumn chunkColumn))
        {
            return chunkColumn;
        }

        return null;
    }

    /**
     * Return count of active columns.
     */
    public uint GetChunkColumnCount()
    {
        return (uint)chunkColumns.Count();
    }
    
    /**
     * Converts world X,Z position to chunk coordinates to which it belongs to.
     */
    public (int chunkX, int chunkZ) WorldPosToChunkCoords(double worldX, double worldZ)
    {
        int chunkX = (int)Math.Floor(worldX / chunkSizeInMeters);
        int chunkZ = (int)Math.Floor(worldZ / chunkSizeInMeters);
        return (chunkX, chunkZ);
    }
    
    /**
     * Creates and returns new chunk column with given X, Z coordinates.
     * Replaces existing one if it exists.
     */
    public ChunkColumn CreateChunkColumn(int chunkX, int chunkZ)
    {
        ChunkColumn chunkColumn = new ChunkColumn(chunkX, chunkZ, chunkCountY, chunkVoxelSize, chunkVoxelSizeMask,
            chunkVoxelSizeShift);
        chunkColumns[(chunkX, chunkZ)] = chunkColumn;
        return chunkColumn;
    }

    /**
     * Removes chunk column with given coordinates from the active columns if it exists.
     * The process also unlinks the column from the interconnected neighbor network of ChunkColumns.
     */
    public void RemoveChunkColumn(int chunkX, int chunkZ)
    {
        ChunkColumn chunkColumn = chunkColumns[(chunkX, chunkZ)];
        if (chunkColumn == null)
        {
            return;
        }

        chunkColumns.Remove((chunkX, chunkZ), out ChunkColumn _);

        ChunkColumn neighbor;
        // clear from neighbor network structure
        if (chunkColumn.HasNeighbor(ORIENTATION.NORTH))
        {
            neighbor = chunkColumn.GetNeighbor(ORIENTATION.NORTH);
            neighbor.columnLock.Enter();
            neighbor.ClearNeighbor(ORIENTATION.SOUTH);
            neighbor.columnLock.Exit();

            chunkColumn.columnLock.Enter();
            chunkColumn.ClearNeighbor(ORIENTATION.NORTH);
            chunkColumn.columnLock.Exit();
        }

        if (chunkColumn.HasNeighbor(ORIENTATION.EAST))
        {
            neighbor = chunkColumn.GetNeighbor(ORIENTATION.EAST);
            neighbor.columnLock.Enter();
            neighbor.ClearNeighbor(ORIENTATION.WEST);
            neighbor.columnLock.Exit();

            chunkColumn.columnLock.Enter();
            chunkColumn.ClearNeighbor(ORIENTATION.EAST);
            chunkColumn.columnLock.Exit();
        }

        if (chunkColumn.HasNeighbor(ORIENTATION.SOUTH))
        {
            neighbor = chunkColumn.GetNeighbor(ORIENTATION.SOUTH);
            neighbor.columnLock.Enter();
            neighbor.ClearNeighbor(ORIENTATION.NORTH);
            neighbor.columnLock.Exit();

            chunkColumn.columnLock.Enter();
            chunkColumn.ClearNeighbor(ORIENTATION.SOUTH);
            chunkColumn.columnLock.Exit();
        }

        if (chunkColumn.HasNeighbor(ORIENTATION.WEST))
        {
            neighbor = chunkColumn.GetNeighbor(ORIENTATION.WEST);
            neighbor.columnLock.Enter();
            neighbor.ClearNeighbor(ORIENTATION.EAST);
            neighbor.columnLock.Exit();

            chunkColumn.columnLock.Enter();
            chunkColumn.ClearNeighbor(ORIENTATION.WEST);
            chunkColumn.columnLock.Exit();
        }
    }
}

/**
 * Orientation for chunk column neighbor indexing.
 */
public enum ORIENTATION : byte
{
    NORTH,
    EAST,
    SOUTH,
    WEST
}

/**
 * Column of chunks in the voxel storage system.
 * Consists of a set of vertically stacked Chunks - root SVO nodes.
 */
public class ChunkColumn
{
    public int chunkCountY { get; }
    public int totalVoxelsY { get; }
    public readonly Lock columnLock;
    public int chunkX { get; }
    public int chunkZ { get; }
    private readonly SVO[] chunks;
    private readonly byte chunkVoxelSize;
    private readonly byte chunkVoxelSizeMask; // for fast modulo operation
    private readonly byte chunkVoxelSizeShift; // for fast division operation
    public ChunkColumn[] neighbors { get; }
    // whether this column is fully generated and ready for meshing
    public bool isGenerated;

    public ChunkColumn(int chunkX, int chunkZ, byte chunkCountY, byte chunkVoxelSize, byte chunkVoxelSizeMask, byte chunkVoxelSizeShift)
    {
        this.chunkCountY = chunkCountY;
        this.chunkX = chunkX;
        this.chunkZ = chunkZ;
        this.chunkVoxelSize = chunkVoxelSize;
        this.chunkVoxelSizeMask = chunkVoxelSizeMask;
        this.chunkVoxelSizeShift = chunkVoxelSizeShift;
        totalVoxelsY = chunkCountY * chunkVoxelSize;
        chunks = new SVO[chunkCountY];
        for (int y = 0; y < chunkCountY; y++)
        {
            chunks[y] = new SVO(chunkVoxelSize);
        }

        isGenerated = false;

        columnLock = new Lock();
        neighbors = new ChunkColumn[4]; // N, E, S, W
    }
    
    /**
     * Connect neighbor from given orientation.
     */
    public void SetNeighbor(ORIENTATION orientation, ChunkColumn neighbor)
    {
        neighbors[(byte)orientation] = neighbor;
    }

    /**
     * Get neighbor connected at orientation.
     */
    public ChunkColumn GetNeighbor(ORIENTATION orientation)
    {
        return neighbors[(byte)orientation];
    }
    
    /**
     * Clear neighbor at given orientation.
     */
    public void ClearNeighbor(ORIENTATION orientation)
    {
        neighbors[(byte)orientation] = null;
    }
    
    /**
     * Whether a neighbor is connected at given orientation.
     */
    public bool HasNeighbor(ORIENTATION orientation)
    {
        return neighbors[(byte)orientation] != null;
    }
    
    /**
     * Whether this column has all neighbors that are fully generated.
     */
    public bool HasAllNeighbors()
    {
        foreach (ChunkColumn neighbor in neighbors)
        {
            if (neighbor == null || !neighbor.IsFullyGenerated())
            {
                return false;
            }
        }
        
        return true;
    }
    
    /**
     * Whether this column is fully generated.
     */
    public bool IsFullyGenerated()
    {
        return isGenerated;
    }
    
    /**
     * Return chunk at given y position. Without boundary check.
     */
    public SVO GetChunk(int chunkY)
    {
        // boundaries not checked
        return chunks[chunkY];
    }
    
    /**
     * Whether chunk at given y position is completely air.
     */
    public bool IsChunkCompletelyAir(uint chunkY)
    {
        SVO chunk = chunks[chunkY];
        // arbitrary local position
        return chunk.GetVoxel(0, 0, 0, chunkVoxelSize) == VoxelType.AIR && !chunk.HasAnyChildren();
    }
    
    /**
     * Sets voxel at given targetSize and position to given type.
     */
    public void SetVoxel(int x, uint y, int z, VoxelType voxelType, byte targetSize = 1)
    {
        uint chunkY = y >> chunkVoxelSizeShift;

        uint localX = (uint) x & chunkVoxelSizeMask;
        uint localY = y & chunkVoxelSizeMask;
        uint localZ = (uint) z & chunkVoxelSizeMask;

        chunks[chunkY].SetVoxel(localX, localY, localZ, voxelType, targetSize);
    }

    /**
     * Sets voxel of given targetSize and position to given type only when it is currently of type AIR or when it is WATER,
     * in which case it only sets PLANT voxels and transforms them into WATER enclosed voxel types so that higher LOD levels
     * do not have empty spots in water geometry.
     */
    public void SetVoxelNoRepaint(int x, uint y, int z, VoxelType voxelType, byte targetSize = 1)
    {
        uint chunkY = y >> chunkVoxelSizeShift;

        uint localX = (uint) x & chunkVoxelSizeMask;
        uint localY = y & chunkVoxelSizeMask;
        uint localZ = (uint) z & chunkVoxelSizeMask;

        chunks[chunkY].SetVoxelNoRepaint(localX, localY, localZ, voxelType, targetSize);
    }

    /**
     * Returns voxel type at given voxel position and at given level of detail (targetSize).
     */
    public VoxelType GetVoxel(int x, uint y, int z, byte targetSize = 1)
    {
        uint chunkY = y >> chunkVoxelSizeShift;

        uint localX = (uint) x & chunkVoxelSizeMask;
        uint localY = y & chunkVoxelSizeMask;
        uint localZ = (uint) z & chunkVoxelSizeMask;

        return chunks[chunkY].GetVoxel(localX, localY, localZ, targetSize);
    }

    /**
     * Whether volume defined by targetSize at given position contains air.
     */
    public bool VolumeContainsAir(int x, uint y, int z, byte targetSize = 1)
    {
        uint chunkY = y >> chunkVoxelSizeShift;

        uint localX = (uint) x & chunkVoxelSizeMask;
        uint localY = y & chunkVoxelSizeMask;
        uint localZ = (uint) z & chunkVoxelSizeMask;

        return chunks[chunkY].ChildrenHaveAir(localX, localY, localZ, targetSize);
    }

    /**
     * Simplifies SVO structure of the column from given y chunk up, to the most space optimized form.
     * Also sets the most prevalent voxel types from children to parents in the SVO structures.
     */
    public void Simplify(uint startY)
    {
        for (uint chunkY = startY; chunkY < chunks.Length; chunkY++)
        {
            chunks[chunkY].Simplify();
        }
    }
}

/**
 * Sparse Voxel Octree node of the voxel storage system.
 * On the node level everything works at locally bound coordinates.
 */
public struct SVO
{
    private readonly byte size; // voxel size of this node level
    private VoxelType type;
    private bool childrenHaveAir;
#nullable enable
    private SVO[]? children;
#nullable disable
    
    public SVO(byte size)
    {
        this.size = size;
        type = VoxelType.AIR;
        children = null; // initially no children
        childrenHaveAir = false;
    }
    
    /**
     * Calculate child index for child at given local coordinates.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChildIndex(uint x, uint y, uint z, byte halfSize)
    {
        return ((x >= halfSize) ? 1 : 0) | 
               ((y >= halfSize) ? 2 : 0) | 
               ((z >= halfSize) ? 4 : 0);
    }

    /**
     * Calculate local coordinates of a child at lower SVO level.
     */
    private static (uint newX, uint newY, uint newZ) GetChildCoordinates(uint x, uint y, uint z, byte halfSize)
    {
        byte mask = (byte)(halfSize - 1); // all is power of two
        return (x & mask, y & mask, z & mask);
    }

    /**
     * Whether this node has any children.
     */
    public bool HasAnyChildren()
    {
        return children == null;
    }

    /**
     * Set this and lower SVO levels to this type
     */
    private void FillUniform(VoxelType type)
    {
        this.type = type;
        children = null; // clear children since this node is now uniform
    }
    
    /**
     * Whether children of this SVO node contain any AIR. Iterative.
     */
    public bool ChildrenHaveAir(uint x, uint y, uint z, uint targetSize = 1)
    {
        SVO current = this;
        uint currentX = x;
        uint currentY = y;
        uint currentZ = z;
        
        while (current.size > targetSize && current.children != null)
        {
            byte halfSize = (byte)(current.size >> 1);
            int index = GetChildIndex(currentX, currentY, currentZ, halfSize);
            
            current = current.children[index];
            (currentX, currentY, currentZ) = GetChildCoordinates(currentX, currentY, currentZ, halfSize);
        }
        return current.childrenHaveAir;
    }

    /**
     * Sets voxel of given targetSize and local coordinates to supplied voxel type.
     * Traverses the SVO structure, updates and creates new necessary nodes and levels.
     * Works recursively.
     */
    public void SetVoxel(uint x, uint y, uint z, VoxelType type, uint targetSize = 1)
    {
        // target level reached, recursion exit
        if (size == targetSize)
        {
            this.type = type;
            children = null; // leaf node, no children
            return;
        }

        byte halfSize = (byte)(size >> 1);
        int index = GetChildIndex(x, y, z, halfSize);

        // allocate children
        if (children == null)
        {
            children = new SVO[8];
            for (int i = 0; i < 8; i++)
            {
                children[i] = new SVO(halfSize);
                children[i].FillUniform(this.type); // copy over type of this SVO node
            }
        }

        // continue deeper
        var (newX, newY, newZ) = GetChildCoordinates(x, y, z, halfSize);
        children[index].SetVoxel(newX, newY, newZ, type, targetSize);
    }

    /**
     * Sets voxel of given targetSize and local coordinates to supplied voxel type only when it is currently of type AIR or when it is WATER,
     * in which case it only sets PLANT voxels and transforms them into WATER enclosed voxel types so that higher LOD levels
     * do not have empty spots in water geometry.
     * Stops traversal when targetSize is reached.
     * Works the same way as regular SetVoxel method.
     * Is recursive.
     */
    public void SetVoxelNoRepaint(uint x, uint y, uint z, VoxelType type, uint targetSize = 1)
    {
        // target level reached
        if (size == targetSize)
        {
            // only set if current type is AIR
            if (this.type == VoxelType.AIR)
            {
                this.type = type;
                children = null; // leaf node, no children
            }
            
            // or when it is WATER and PLANT voxel is being placed
            if (this.type == VoxelType.WATER)
            {
                // transform PLANT voxels into WATER enclosed variants
                switch (type)
                {
                    case VoxelType.PLANT:
                        this.type = VoxelType.PLANT_WATER;
                        children = null;
                        break;
                    case VoxelType.PLANT_DARK:
                        this.type = VoxelType.PLANT_DARK_WATER;
                        children = null;
                        break;
                    case VoxelType.YELLOW:
                        this.type = VoxelType.YELLOW_WATER;
                        children = null;
                        break;
                    case VoxelType.BROWN:
                        this.type = VoxelType.BROWN_WATER;
                        children = null;
                        break;
                    case VoxelType.RED:
                        this.type = VoxelType.RED_WATER;
                        children = null;
                        break;
                    case VoxelType.BLUE:
                        this.type = VoxelType.BLUE_WATER;
                        children = null;
                        break;
                    case VoxelType.WHITE:
                        this.type = VoxelType.WHITE_WATER;
                        children = null;
                        break;
                    default:
                        // do nothing if not a plant
                        break;
                }
            }
            
            return;
        }

        byte halfSize = (byte)(size >> 1);
        int index = GetChildIndex(x, y, z, halfSize);
        
        if (children == null)
        {
            // skip if not AIR or WATER, early exit
            if (this.type != VoxelType.AIR && this.type != VoxelType.WATER)
            {
                return;
            }
            
            // otherwise allocate children
            children = new SVO[8];
            for (int i = 0; i < 8; i++)
            {
                children[i] = new SVO(halfSize);
                children[i].FillUniform(this.type); // copy over type of this node
            }
        }

        // continue deeper
        var (newX, newY, newZ) = GetChildCoordinates(x, y, z, halfSize);
        children[index].SetVoxelNoRepaint(newX, newY, newZ, type, targetSize);
    }

    /**
     * Returns voxel type of given size and local coordinates. Runs iteratively.
     */
    public VoxelType GetVoxel(uint x, uint y, uint z, uint targetSize = 1)
    {
        SVO current = this;
        uint currentX = x;
        uint currentY = y;
        uint currentZ = z;
        
        while (current.size > targetSize && current.children != null)
        {
            byte halfSize = (byte)(current.size >> 1);
            int index = GetChildIndex(currentX, currentY, currentZ, halfSize);
            
            current = current.children[index];
            (currentX, currentY, currentZ) = GetChildCoordinates(currentX, currentY, currentZ, halfSize);
        }
        
        return current.type;
    }

    /**
     * Recursively simplifies this SVO nodes and its children to the most space optimized form.
     * During the process the most prevalent children voxel types are send upwards and
     * set as voxel types of parents.
     */
    public (bool simplified, bool doChildrenHaveAir) Simplify()
    {
        if (children == null)
        {
            return (false, false);
        }

        // simplify children recursively
        bool childrenSimplified = false;
        for (int i = 0; i < 8; i++)
        {
            if (children[i].Simplify().simplified)
            {
                childrenSimplified = true;
            }
        }

        // check if this node can be simplified
        bool canSimplify = true;
        // type counts to find the most prevalent type among children
        Dictionary<VoxelType, byte> typeCounts = new Dictionary<VoxelType, byte>();

        for (int i = 0; i < 8; i++)
        {
            if (children[i].children != null)
            {
                // child has children, cannot simplify
                canSimplify = false;
            }
            
            VoxelType childType = children[i].type;

            if (!typeCounts.TryAdd(childType, 1))
                typeCounts[childType]++;
        }

        canSimplify &= typeCounts.Count == 1;
        
        // do children have air
        childrenHaveAir = typeCounts.ContainsKey(VoxelType.AIR);
        
        typeCounts.Remove(VoxelType.AIR);

        // set prevalent type among children to this parent
        if (typeCounts.Count > 0)
        {
            type = typeCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        else
        { // had to be AIR which was removed
            type = VoxelType.AIR;
        }
        
        if (canSimplify)
        {
            children = null;
            return (true, childrenHaveAir);
        }

        return (childrenSimplified, childrenHaveAir);
    }
}
