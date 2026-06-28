// FILE: WorldRegion.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains WorldRegion class and its dependencies. Internal mostly 2D pre-voxel representation
//          of the generated world divided into large symmetrical areas.

using System;
using System.Collections.Generic;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Structure holding information about a placement of vegetation or decoration model within a WorldRegion.
 * Contains link to model voxel data and terrain vertical placement offset.
 */
public struct FeaturePlacement
{
    public readonly ushort posX; // local in region voxel units
    public readonly ushort posZ; // local in region voxel units
    public readonly VoxelItem[] featureVoxels; // model voxel data
    public readonly short offsetY; // vertical offset against terrain height at given XZ position
    
    // CONSTRUCTORS
    
    public FeaturePlacement(ushort posX, ushort posZ, Vegetation vegetation)
    {
        this.posX = posX;
        this.posZ = posZ;
        featureVoxels = vegetation.voxels;
        offsetY = vegetation.offsetY;
    }
    
    public FeaturePlacement(ushort posX, ushort posZ, Vegetation vegetation, short offsetY)
    {
        this.posX = posX;
        this.posZ = posZ;
        featureVoxels = vegetation.voxels;
        this.offsetY = offsetY;
    }

    public FeaturePlacement(ushort posX, ushort posZ, Rock rock)
    {
        this.posX = posX;
        this.posZ = posZ;
        featureVoxels = rock.voxels;
        offsetY = rock.offsetY;
    }
    
    public FeaturePlacement(ushort posX, ushort posZ, Trunk trunk)
    {
        this.posX = posX;
        this.posZ = posZ;
        featureVoxels = trunk.voxels;
        offsetY = trunk.offsetY;
    }
}

/**
 * Structure holding water availability at specified position.
 * Used for sharing of water availability influence across region boundaries.
 */
public struct WaterEffect(ushort posX, ushort posZ, byte value)
{
    public readonly ushort posX = posX;
    public readonly ushort posZ = posZ;
    public readonly byte value = value;
}

/**
 * Segment of the interconnecting cave tunnel system.
 */
public class CaveTunnelSegment(Vector3Int start, Vector3Int end, float radius)
{
    public Vector3Int start = start;
    public Vector3Int end = end;
    public readonly float radius = radius;
}

/**
 * Stores coordinates of a single voxel that needs to be replaced with AIR in VoxelGenerator when its
 * chunk column will be voxelized.
 * Necessary for storing pre-voxelized per column cave data, as the WorldRegion contains only 2D representations.
 */
public struct CaveCarveOperation(ushort x, ushort y, ushort z)
{
    public readonly ushort x = x;
    public readonly ushort y = y;
    public readonly ushort z = z;
}

/**
 * Segment / region storing internal representations of the world generation process at the pre-voxel level.
 * Stores data from both steps of WorldGenerator - terrain and features.
 * Data includes height map, water map, water availabilities, snow placement, ground voxel types and placements of
 * vegetation and decorative features of the region. Also stores pre-voxelized cave tunnel data as voxel carving operations.
 */
public class WorldRegion
{
    public ushort size { get; } // height and width size in terrain units
    public readonly int posX;
    public readonly int posZ;
    public bool firstStepDone; // only terrain step was finished
    public bool ready; // both terrain and feature generation steps are done
    
    // FIRST GENERATION STEP
    // heightMap, disabledPositionMap, caveCarveOperationsInColumns, waterMap, neighborWaterEffects towards 8 neighboring columns
    private readonly ushort[,] heightMap;
    private readonly HashSet<(ushort x, ushort z)> disabledPositionMap;
    private readonly List<CaveCarveOperation>[,] caveCarveOperationsInColumns; // array of pre-voxelized carve operations for cave tunnels in each column
    // waterMap
    // 0 signals no water at given location
    // positive values signal number of water blocks stacked on top of heightmap at given location
    // negative values signal depth of water below heightmap at given location
    // stored in byte with offset of + 128, 0-127 below, 128 zero (not stored), 129-255 above
    private readonly Dictionary<(ushort posX, ushort posZ), byte> waterMap;
    private readonly Dictionary<(short neighborX, short neihgborZ), WaterEffect[]> neighborWaterEffects; // for neighbor X, Z regions
    
    // SECOND GENERATION STEP
    // complete waterAvailabilityMap, snowMap, groundVoxelTypeMap and featuresInColumns placements
    private readonly byte[,] waterAvailabilityMap; // includes effects from neighboring regions after generation is finished
    private readonly HashSet<(ushort x, ushort z)> snowMap;
    private readonly VoxelType[,] groundVoxelTypeMap;
    private readonly List<FeaturePlacement>[,] featuresInColumns; // array of lists for feature placement in each column
    
    public WorldRegion(ushort size, int columnsInRegionOneAxis, int posX, int posZ)
    {
        this.size  = size;
        firstStepDone = false;
        ready = false;
        this.posX = posX;
        this.posZ = posZ;
        heightMap = new ushort[size, size];
        disabledPositionMap = [];
        waterMap = new Dictionary<(ushort, ushort), byte>();
        neighborWaterEffects = new Dictionary<(short, short), WaterEffect[]>();
        waterAvailabilityMap = new byte[size, size];
        snowMap = [];
        groundVoxelTypeMap = new VoxelType[size, size];
        featuresInColumns = new List<FeaturePlacement>[columnsInRegionOneAxis, columnsInRegionOneAxis];
        caveCarveOperationsInColumns = new List<CaveCarveOperation>[columnsInRegionOneAxis, columnsInRegionOneAxis];
        for (int x = 0; x < columnsInRegionOneAxis; x++)
        {
            for (int z = 0; z < columnsInRegionOneAxis; z++)
            {
                featuresInColumns[x, z] = [];
                caveCarveOperationsInColumns[x, z] = [];
            }
        }
    }
    
    /**
     * Returns terrain height at supplied local coordinates.
     */
    public ushort GetHeightAtLocalCoords(ushort localX, ushort localZ)
    {
        return heightMap[localX, localZ];
    }
    
    /**
     * Returns water level at supplied local coordinates.
     */
    public short GetWaterLevelAtLocalCoords(ushort localX, ushort localZ)
    {
        // necessary to offset by 128
        return waterMap.TryGetValue((localX, localZ), out byte waterLevel) ? (short)(waterLevel - 128) : (short)0;
    }
    
    /**
     * Returns water availability at supplied local coordinates.
     */
    public byte GetWaterAvailabilityAtLocalCoords(ushort localX, ushort localZ)
    {
        return waterAvailabilityMap[localX, localZ];
    }
    
    /**
     * Sets terrain height at given coordinates.
     */
    public void SetHeight(ushort localX, ushort localZ, ushort height)
    {
        heightMap[localX, localZ] = height;
    }
    
    /**
     * Sets water level at given coordinates.
     */
    public void SetWaterLevel(ushort localX, ushort localZ, short waterLevel)
    {
        if (waterLevel == 0)
            return; // zero values not stored
        // necessary to offset by 128
        short clampedWaterLevel = Math.Clamp(waterLevel, (short)-128, (short)127);
        byte storedWaterLevel = (byte)(clampedWaterLevel + 128);
        waterMap[(localX, localZ)] = storedWaterLevel;
    }
    
    /**
     * Sets water availability at supplied local position to given value directly.
     */
    public void SetWaterAvailability(ushort localX, ushort localZ, byte amount)
    {
        waterAvailabilityMap[localX, localZ] = amount;
    }
    
    /**
     * Saves array with water effects for given X, Z neighbor.
     */
    public void SetNeighborWaterEffects(short neighborX, short neighborZ, WaterEffect[] effects)
    {
        neighborWaterEffects[(neighborX, neighborZ)] = effects;
    }
    
    /**
     * Returns array with water effects for given X, Z neighbor.
     */
    public WaterEffect[] GetNeighborWaterEffects(short neighborX, short neighborZ)
    {
        if (neighborWaterEffects.TryGetValue((neighborX, neighborZ), out WaterEffect[] effects))
            return effects;
        return null;
    }
    
    /**
     * Returns voxel type for ground at given local position.
     */
    public VoxelType GetGroundVoxelTypeAtLocalCoords(ushort localX, ushort localZ)
    {
        return groundVoxelTypeMap[localX, localZ];
    }
    
    /**
     * Sets voxel type for ground at given local position.
     */
    public void SetGroundVoxelType(ushort localX, ushort localZ, VoxelType voxelType)
    {
        groundVoxelTypeMap[localX, localZ] = voxelType;
    }
    
    /**
     * Returns true if given ground position should be covered with snow.
     */
    public bool HasSnowAtLocalCoords(ushort localX, ushort localZ)
    {
        return snowMap.Contains((localX, localZ));
    }
    
    /**
     * Sets ground position to be covered with snow.
     */
    public void SetSnowAtLocalCoords(ushort localX, ushort localZ)
    {
        snowMap.Add((localX, localZ));
    }
    
    /**
     * Adds feature placement into given world region column.
     */
    public void AddFeature(FeaturePlacement feature, ushort colXInRegion, ushort colZInRegion)
    {
        featuresInColumns[colXInRegion, colZInRegion].Add(feature);
    }
    
    /**
     * Returns all features that should be placed into given world region column.
     */
    public List<FeaturePlacement> GetFeaturesInColumn(ushort colXInRegion, ushort colZInRegion)
    {
        return featuresInColumns[colXInRegion, colZInRegion];
    }
    
    /**
     * Adds cave carve operation to given world region column.
     */
    public void AddCaveCarveOperation(CaveCarveOperation operation, ushort colXInRegion, ushort colZInRegion)
    {
        caveCarveOperationsInColumns[colXInRegion, colZInRegion].Add(operation);
    }
    
    /**
     * Returns all carving operations that should be done in specified world region column.
     */
    public List<CaveCarveOperation> GetCaveCarveOperationsInColumn(ushort colXInRegion, ushort colZInRegion)
    {
        return caveCarveOperationsInColumns[colXInRegion, colZInRegion];
    }
    
    /**
     * Returns true if given local position was disabled for placement of features.
     */
    public bool IsPositionDisabled(ushort localX, ushort localZ)
    {
        return disabledPositionMap.Contains((localX, localZ));
    }
    
    /**
     * Sets local position to be disabled for feature placement.
     */
    public void SetPositionDisabled(ushort localX, ushort localZ)
    {
        disabledPositionMap.Add((localX, localZ));
    }
}
