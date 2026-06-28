// FILE: VoxelGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains VoxelGenerator class and its dependencies that voxelize region data into smaller voxel chunk columns.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents a placement of stalactite or stalagmite (direction).
 */
public struct StalactitePosition(int x, uint y, int z, STALACTITE_GROWTH_DIR growthDir)
{
    public readonly int x = x;
    public readonly uint y = y;
    public readonly int z = z;
    public readonly STALACTITE_GROWTH_DIR growthDir = growthDir;
}

/**
 * Encapsulates parameters for the VoxelGenerator.
 */
public struct VoxelGeneratorParams
{
    public VoxelStorage voxelStorage { get; init; }
    public WorldGeneratorService worldGeneratorService  { get; init; }
    public StorageService storageService { get; init; }
    public CaveGenerationHelper caveGenerationHelper { get; init; }
    public int seed { get; init; }
    public int metersPerRegion { get; init; }
    public int metersPerChunk { get; init; }
    public int voxelsPerMeter { get; init; }
    public int terrainUnitsPerMeter { get; init; }
    public ushort caveStartYLevelTerrainUnits { get; init; }
    public ushort cavesWaterHeightTerrainUnits { get; init; }
    public bool cavesEnabled { get; init; }
    public int stalactiteChance { get; init; }
    public int waterfallChance { get; init; }
}

/**
 * Voxel generator class that voxelizes columns of chunks based on prepared data in WorldRegion structures.
 * Includes cave generation, that apart from generation and pre-voxelization of tunnel networks happens solely at this stage.
 */
public class VoxelGenerator
{
    // dependencies
    private readonly CaveGenerationHelper caveGenerationHelper;
    private readonly VoxelStorage voxelStorage;
    private readonly WorldGeneratorService worldGeneratorService;
    private readonly StorageService storageService;
    
    private readonly int seed;
    // world and voxel configuration
    private readonly int metersPerRegion;
    private readonly int metersPerChunk;
    private readonly int chunkVoxelSize;
    private readonly int chunkTerrainUnits;
    private readonly byte normalToTerrainRatio;
    private readonly int regionSizeInVoxels;
    private readonly int regionSizeInTerrainUnits;
    private readonly int terrainUnitsVoxelsRatio;

    private readonly bool cavesEnabled;
    private readonly int stalagmiteChance; // of 1000
    private readonly int stalactiteChance; // of 1000
    private readonly int stalactiteChunkColumnBounds = 2;
    private readonly int waterfallChance; // of 1000
    private readonly ushort caveStartYLevelTerrainUnits; // height at which caves begin generation
    private readonly ushort cavesWaterHeightTerrainUnits; // predefined height of water in the caves
    
    // stalactite / stalagmite models
    private readonly List<Stalactite> stalactitesUp = [];
    private readonly List<Stalactite> stalactitesDown = [];

    public VoxelGenerator(VoxelGeneratorParams parameters)
    {
        voxelStorage = parameters.voxelStorage;
        worldGeneratorService = parameters.worldGeneratorService;
        storageService = parameters.storageService;
        caveGenerationHelper = parameters.caveGenerationHelper;
        
        cavesWaterHeightTerrainUnits = parameters.cavesWaterHeightTerrainUnits;
        seed = parameters.seed; 
        metersPerRegion = parameters.metersPerRegion;
        metersPerChunk = parameters.metersPerChunk;
        caveStartYLevelTerrainUnits = parameters.caveStartYLevelTerrainUnits;
        chunkVoxelSize = metersPerChunk * parameters.voxelsPerMeter;
        chunkTerrainUnits = parameters.terrainUnitsPerMeter * metersPerChunk;
        normalToTerrainRatio = (byte)(parameters.voxelsPerMeter / parameters.terrainUnitsPerMeter);
        regionSizeInVoxels = metersPerRegion * parameters.voxelsPerMeter;
        regionSizeInTerrainUnits = metersPerRegion * parameters.terrainUnitsPerMeter;
        terrainUnitsVoxelsRatio = parameters.voxelsPerMeter / parameters.terrainUnitsPerMeter;
        cavesEnabled = parameters.cavesEnabled;
        stalactiteChance = parameters.stalactiteChance;
        stalagmiteChance = stalactiteChance / 2; // lower chance, player can move better on the ground
        waterfallChance = parameters.waterfallChance;
    }
    
    /**
     * Adds stalactite and stalagmite (down, up) models so that they can be used by the voxel generator.
     */
    public void AddStalactiteModels(ConcurrentBag<Stalactite> stalactites)
    {
        foreach (Stalactite stalactite in stalactites)
        {
            if (stalactite.growthDir == STALACTITE_GROWTH_DIR.UP)
            {
                stalactitesUp.Add(stalactite);
            }
            else
            {
                stalactitesDown.Add(stalactite);
            }
        }
    }

    /**
     * Calculates region coordinates where belongs given column.
     */
    private (int regionX, int regionZ) GetRegionForChunk(int chunkX, int chunkZ)
    {
        int regionX = (int)Math.Floor(chunkX * metersPerChunk / (float)metersPerRegion);
        int regionZ = (int)Math.Floor(chunkZ * metersPerChunk / (float)metersPerRegion);
        return (regionX, regionZ);
    }

    /**
     * Covers the full generation / voxelization process of a single chunk column.
     * Firstly the chunk column is build up to the terrain height level. In the space below is generated
     * a system of caves including stalactites, stalagmites placement and waterfalls.
     * Tunnel carving operations prepared by the WorldGenerator are also applied.
     * After that it places all voxels of feature models planned for this chunk column. Including voxels
     * from features in this column neighbors that cross column boundaries and leak into this one.
     * Last step is application of snow on top of the highest positions and application of voxel edit operations
     * stored on the disk from previous user edits.
     */
    public ChunkColumn GenerateChunkColumn(ChunkColumnTask task)
    {
        // obtain this column region and calculate region offset
        var (regionX, regionZ) = GetRegionForChunk(task.chunkX, task.chunkZ);
        int regionOffsetX = (regionX * metersPerRegion) / metersPerChunk;
        int regionOffsetZ = (regionZ * metersPerRegion) / metersPerChunk;
        WorldRegion region = worldGeneratorService.GetRegionWhenReady(regionX, regionZ);
        if (region == null)
        {
            GD.PrintErr($"Chunk col {task.chunkX},{task.chunkZ} cannot be generated, dependency region {regionX},{regionZ} not loaded");
            return null;
        }

        Dictionary<(int, int), ChunkColumn> neighboringColumns = new Dictionary<(int, int), ChunkColumn>();
        // obtain all neighboring columns (1 in each direction)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                neighboringColumns[(dx, dz)] = voxelStorage.GetChunkColumn(task.chunkX + dx, task.chunkZ + dz);
                if (neighboringColumns[(dx, dz)] == null)
                {
                    GD.PrintErr($"Chunk col {task.chunkX},{task.chunkZ} cannot be generated, dependency col {task.chunkX + dx},{task.chunkZ + dz} not loaded");
                    return null;
                }
            }
        }
        
        ChunkColumn chunkColumn = neighboringColumns[(0, 0)]; // center is this column
        neighboringColumns.Remove((0, 0));
        // configure neighbor columns
        ChunkColumn neighbor = neighboringColumns[(0, 1)]; // Z+
        chunkColumn.SetNeighbor(ORIENTATION.NORTH, neighbor);
        neighbor.SetNeighbor(ORIENTATION.SOUTH, chunkColumn);

        neighbor = neighboringColumns[(0, -1)]; // Z-
        chunkColumn.SetNeighbor(ORIENTATION.SOUTH, neighbor);
        neighbor.SetNeighbor(ORIENTATION.NORTH, chunkColumn);

        neighbor = neighboringColumns[(1, 0)]; // X+
        chunkColumn.SetNeighbor(ORIENTATION.EAST, neighbor);
        neighbor.SetNeighbor(ORIENTATION.WEST, chunkColumn);

        neighbor = neighboringColumns[(-1, 0)]; // X-
        chunkColumn.SetNeighbor(ORIENTATION.WEST, neighbor);
        neighbor.SetNeighbor(ORIENTATION.EAST, chunkColumn);

        int startX = chunkColumn.chunkX * chunkTerrainUnits;
        int startZ = chunkColumn.chunkZ * chunkTerrainUnits;
        int endX = startX + chunkTerrainUnits;
        int endZ = startZ + chunkTerrainUnits;

        Random random = new Random(seed); // initialize random number generator for generation of cave decorations
        
        List<StalactitePosition> stalactitePositions = [];

        // generate this column in small sub columns in the XZ space of terrain unit size
        for (int x = startX; x < endX; x++)
        {
            int actualX = x * normalToTerrainRatio;
            ushort inRegionX = (ushort)((chunkColumn.chunkX - regionOffsetX) * chunkTerrainUnits + (x - startX));
            for (int z = startZ; z < endZ; z++)
            {
                int actualZ = z * normalToTerrainRatio;
                ushort inRegionZ = (ushort)((chunkColumn.chunkZ - regionOffsetZ) * chunkTerrainUnits + (z - startZ));
                // obtain generated parameters
                ushort height = region.GetHeightAtLocalCoords(inRegionX, inRegionZ);
                short water = region.GetWaterLevelAtLocalCoords(inRegionX, inRegionZ);
                VoxelType groundLayerType = region.GetGroundVoxelTypeAtLocalCoords(inRegionX, inRegionZ);

                // calculate water addition or carving in terrain
                ushort waterUpTo;
                ushort maxY;
                if (water < 0)
                {
                    maxY = (ushort)(height + water);
                    waterUpTo = height;
                }
                else
                {
                    maxY = height;
                    waterUpTo = (ushort)(height + water);
                }

                // obtain count of grassy blocks on top, dirt blocks below it and the rest stone blocks
                ushort grassLayerBlocks = 2;
                ushort dirtLayerBlocks = 2;
                ushort stoneLayerBlocksUp = 8;
                ushort stoneLayerBlocksDown = caveStartYLevelTerrainUnits;

                uint y;

                if (groundLayerType == VoxelType.STONE)
                {
                    grassLayerBlocks = 0;
                    dirtLayerBlocks = 0;
                }

                // apply water
                for (y = maxY; y < waterUpTo; y++)
                {
                    uint actualY = y * normalToTerrainRatio;
                    chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.WATER, normalToTerrainRatio);
                }

                // need to correct the counts based on actual world height at this position and apply them

                // correct, apply ground voxel type
                grassLayerBlocks = Math.Min(grassLayerBlocks, maxY);
                maxY -= grassLayerBlocks;
                for (y = maxY; y < (maxY + grassLayerBlocks); y++)
                {
                    uint actualY = y * normalToTerrainRatio;
                    chunkColumn.SetVoxel(actualX, actualY, actualZ, groundLayerType, normalToTerrainRatio);
                }

                // correct, apply dirt
                dirtLayerBlocks = Math.Min(dirtLayerBlocks, maxY);
                maxY -= dirtLayerBlocks;
                for (y = maxY; y < (maxY + dirtLayerBlocks); y++)
                {
                    uint actualY = y * normalToTerrainRatio;
                    chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.DIRT, normalToTerrainRatio);
                }

                // correct, apply minimal stone layer
                stoneLayerBlocksUp = Math.Min(stoneLayerBlocksUp, maxY);
                maxY -= stoneLayerBlocksUp;
                for (y = maxY; y < (maxY + stoneLayerBlocksUp); y++)
                {
                    uint actualY = y * normalToTerrainRatio;
                    chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.STONE, normalToTerrainRatio);
                }
                
                // correct and apply stone layer at the bottom of the world
                stoneLayerBlocksDown = Math.Min(stoneLayerBlocksDown, maxY);
                for (y = 0; y < stoneLayerBlocksDown; y++)
                {
                    uint actualY = y * normalToTerrainRatio;
                    chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.STONE_DARKER, normalToTerrainRatio);
                }

                // calculate lowerLimit, check whether there is space for caves
                short totalCaveSpace = (short)(maxY - stoneLayerBlocksDown);
                if (totalCaveSpace <= 0)
                    continue; // no space for caves

                ushort lowerLimit = (ushort)(maxY - totalCaveSpace);
                bool isWaterfall = false;
                bool lastWasStone = true;

                // process cave space
                if (!cavesEnabled)
                {
                    // fill cave space with stone, no caves
                    for (y = lowerLimit; y < maxY; y++)
                    {
                        uint actualY = y * normalToTerrainRatio;
                        chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.STONE, normalToTerrainRatio);
                    }

                    continue;
                }

                for (y = lowerLimit; y < maxY; y++)
                {
                    // 3D noise based cave space generation
                    uint actualY = y * normalToTerrainRatio;
                    if (!caveGenerationHelper.IsCave(actualX, actualY, actualZ))
                    {
                        VoxelType stoneType = caveGenerationHelper.GetCaveTerrainType(y);
                        chunkColumn.SetVoxel(actualX, actualY, actualZ, stoneType, normalToTerrainRatio);
                        // if there is a waterfall, it ends with the cave roof of this space
                        isWaterfall = false;
                        if (!lastWasStone)
                        {
                            // cave roof - chance to spawn stalactite heading DOWN
                            lastWasStone = true;
                            
                            int chance = random.Next(0, 1000);
                            // check chance and position within bounds
                            if (chance < stalactiteChance &&
                                y > lowerLimit + cavesWaterHeightTerrainUnits &&
                                x > startX + stalactiteChunkColumnBounds &&
                                x < endX - stalactiteChunkColumnBounds &&
                                z > startZ + stalactiteChunkColumnBounds &&
                                z < endZ - stalactiteChunkColumnBounds)
                            {
                                stalactitePositions.Add(new StalactitePosition(actualX, actualY, actualZ, STALACTITE_GROWTH_DIR.DOWN));
                            }
                        }
                    }
                    else
                    {
                        if (lastWasStone)
                        {
                            // cave floor - chance to spawn stalactite heading UP
                            lastWasStone = false;
                            
                            int chance = random.Next(0, 1000);
                            // check chance and position within bounds
                            if (chance < stalagmiteChance &&
                                y > lowerLimit + cavesWaterHeightTerrainUnits &&
                                x > startX + stalactiteChunkColumnBounds &&
                                x < endX - stalactiteChunkColumnBounds &&
                                z > startZ + stalactiteChunkColumnBounds &&
                                z < endZ - stalactiteChunkColumnBounds)
                            {
                                stalactitePositions.Add(new StalactitePosition(actualX, actualY, actualZ, STALACTITE_GROWTH_DIR.UP));
                            }
                        }

                        // set predefined water at the bottom of caves
                        if (y < lowerLimit + cavesWaterHeightTerrainUnits)
                        {
                            chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.WATER, normalToTerrainRatio);
                        }
                        else if (isWaterfall)
                        {
                            chunkColumn.SetVoxel(actualX, actualY, actualZ, VoxelType.WATER, normalToTerrainRatio);
                        }
                    }

                    // there is a chance that a waterfall will be generated at this spot - only at the top of predefined water levels
                    if (y == (lowerLimit + cavesWaterHeightTerrainUnits - 1))
                    {
                        int chance = random.Next(0, 1000);
                        if (chance < waterfallChance)
                        {
                            isWaterfall = true;
                        }
                    }
                }

                // also add stalactites for the roofs of caves that are the end of the world surface
                if (!lastWasStone)
                {
                    uint actualY = y * normalToTerrainRatio;
                    int chance = random.Next(0, 1000);
                    // check chance and bounds
                    if (chance < stalactiteChance &&
                        y > lowerLimit + cavesWaterHeightTerrainUnits &&
                        x > startX + stalactiteChunkColumnBounds &&
                        x < endX - stalactiteChunkColumnBounds &&
                        z > startZ + stalactiteChunkColumnBounds &&
                        z < endZ - stalactiteChunkColumnBounds)
                    {
                        stalactitePositions.Add(new StalactitePosition(actualX, actualY, actualZ, STALACTITE_GROWTH_DIR.DOWN));
                    }
                }
            }
        }
        // calculates region origin position in voxel units
        int regionOriginXVoxels = region.posX * regionSizeInVoxels;
        int regionOriginZVoxels = region.posZ * regionSizeInVoxels;

        // carve out cave tunnels
        List<CaveCarveOperation> carveOperations = region.GetCaveCarveOperationsInColumn((ushort)(chunkColumn.chunkX - regionOffsetX), (ushort)(chunkColumn.chunkZ - regionOffsetZ));
        ApplyCaveCarveOperations(chunkColumn,  carveOperations, regionOriginXVoxels, regionOriginZVoxels);
        
        // apply stalactite placements
        ApplyStalactites(chunkColumn, stalactitePositions, random);

        // column bounds in voxel coordinates
        int columnStartX = chunkColumn.chunkX * chunkVoxelSize;
        int columnEndX = columnStartX + chunkVoxelSize;
        int columnStartZ = chunkColumn.chunkZ * chunkVoxelSize;
        int columnEndZ = columnStartZ + chunkVoxelSize;

        // place features meant for this column
        List<FeaturePlacement> features = region.GetFeaturesInColumn((ushort)(chunkColumn.chunkX - regionOffsetX), (ushort)(chunkColumn.chunkZ - regionOffsetZ));
        ApplyFeaturesIntoColumn(chunkColumn, features, region, regionOriginXVoxels, regionOriginZVoxels, columnStartX, columnEndX, columnStartZ, columnEndZ);
        
        // apply voxels of features that leak from neighboring columns into this one
        foreach (ChunkColumn neighborColumn in neighboringColumns.Values)
        {
            // skip current column, already applied
            if (neighborColumn.chunkX == chunkColumn.chunkX && neighborColumn.chunkZ == chunkColumn.chunkZ)
                continue;

            // calculate region coordinates of the neighbor column and its position in its region
            int colRegionX = (int)Math.Floor(neighborColumn.chunkX * metersPerChunk / (float)metersPerRegion);
            int colRegionZ = (int)Math.Floor(neighborColumn.chunkZ * metersPerChunk / (float)metersPerRegion);
            int colRegionOffsetX = (colRegionX * metersPerRegion) / metersPerChunk;
            int colRegionOffsetZ = (colRegionZ * metersPerRegion) / metersPerChunk;
            WorldRegion colRegion = worldGeneratorService.GetRegionWhenReady(colRegionX, colRegionZ);
            if (colRegion == null)
            {
                GD.PrintErr($"Cannot apply neighbor column feature leakage from {neighborColumn.chunkX},{neighborColumn.chunkZ}, its region {colRegionX},{colRegionZ} is not loaded");
                continue;
            }
            
            // different origin
            regionOriginXVoxels = colRegion.posX * regionSizeInVoxels;
            regionOriginZVoxels = colRegion.posZ * regionSizeInVoxels;

            List<FeaturePlacement> colFeatures = colRegion.GetFeaturesInColumn((ushort)(neighborColumn.chunkX - colRegionOffsetX), (ushort)(neighborColumn.chunkZ - colRegionOffsetZ));
            ApplyFeaturesIntoColumn(chunkColumn, colFeatures, colRegion, regionOriginXVoxels, regionOriginZVoxels, columnStartX, columnEndX, columnStartZ, columnEndZ);
        }

        // apply snow
        ApplySnow(chunkColumn);
        
        // load user changes saved in file
        storageService.LoadApplyChunkColumnVoxelOperations(chunkColumn);
        
        chunkColumn.Simplify(0);
        chunkColumn.isGenerated = true;
        return chunkColumn;
    }

    /**
     * Apply voxels of the supplied feature models into this column.
     */
    private void ApplyFeaturesIntoColumn(ChunkColumn chunkColumn, List<FeaturePlacement> features, WorldRegion region, int regionOriginXVoxels, int regionOriginZVoxels, int colStartX, int colEndX, int colStartZ, int colEndZ)
    {
        foreach (FeaturePlacement feature in features)
        {
            VoxelItem[] plantModel = feature.featureVoxels;

            // convert vegetation position from region-local voxel coordinates to world voxel coordinates
            int worldVegPosX = regionOriginXVoxels + feature.posX;
            int worldVegPosZ = regionOriginZVoxels + feature.posZ;
            // convert vegetation position to terrain units for height map access
            ushort vegTerrainX = (ushort)(feature.posX / (uint)terrainUnitsVoxelsRatio);
            ushort vegTerrainZ = (ushort)(feature.posZ / (uint)terrainUnitsVoxelsRatio);
            // bounds check
            if (vegTerrainX >= regionSizeInTerrainUnits || vegTerrainZ >= regionSizeInTerrainUnits)
            {
                GD.PrintErr($"Vegetation at region voxel ({feature.posX}, {feature.posZ}) that converts to terrain ({vegTerrainX}, {vegTerrainZ}) is out of bounds");
                continue;
            }

            // get height
            ushort modelHeight = region.GetHeightAtLocalCoords(vegTerrainX, vegTerrainZ);
            uint modelHeightTrans = (uint)(modelHeight * normalToTerrainRatio + feature.offsetY);

            // go through all model voxels and set them in appropriate chunk columns
            foreach (VoxelItem voxel in plantModel)
            {
                int worldX = worldVegPosX + voxel.X;
                int worldZ = worldVegPosZ + voxel.Z;

                // only apply to this column
                if (worldX < colStartX || worldX >= colEndX || worldZ < colStartZ || worldZ >= colEndZ)
                {
                    continue;
                }

                uint worldY = (uint)(modelHeightTrans + voxel.Y);
                chunkColumn.SetVoxelNoRepaint(worldX, worldY, worldZ, voxel.Type, 1);
            }
        }
    }

    /**
     * Places stalactites to the column at specified positions. Actual stalactite models are chosen randomly.
     */
    private void ApplyStalactites(ChunkColumn chunkColumn, List<StalactitePosition> stalactitePositions, Random random)
    {
        // apply stalactites
        foreach (StalactitePosition stalactitePos in stalactitePositions)
        {
            VoxelItem[] stalactiteModel = null;
            // obtain stalactite voxel model to place
            switch (stalactitePos.growthDir)
            {
                case STALACTITE_GROWTH_DIR.UP:
                    // check space below, if it is AIR or WATER, skip
                    VoxelType belowType = chunkColumn.GetVoxel(stalactitePos.x, stalactitePos.y - normalToTerrainRatio, stalactitePos.z);
                    if (belowType == VoxelType.AIR || belowType == VoxelType.WATER)
                        continue;

                    if (stalactitesUp.Count == 0)
                        break;
                    // randomly select
                    stalactiteModel = stalactitesUp[random.Next(0, stalactitesUp.Count)].voxels;
                    break;
                
                case STALACTITE_GROWTH_DIR.DOWN:
                    // check space above, if it is AIR, skip
                    VoxelType aboveType = chunkColumn.GetVoxel(stalactitePos.x, stalactitePos.y + normalToTerrainRatio, stalactitePos.z);
                    if (aboveType == VoxelType.AIR)
                        continue;
                    
                    if (stalactitesDown.Count == 0)
                        break;
                    // randomly select
                    stalactiteModel = stalactitesDown[random.Next(0, stalactitesDown.Count)].voxels;
                    break;
            }

            // place the selected stalactite model
            if (stalactiteModel != null)
            {
                foreach (VoxelItem voxel in stalactiteModel)
                {
                    int worldX = stalactitePos.x + voxel.X + 2;
                    uint worldY = (uint)(stalactitePos.y + voxel.Y);
                    int worldZ = stalactitePos.z + voxel.Z + 2;

                    chunkColumn.SetVoxel(worldX, worldY, worldZ, voxel.Type, 1);
                }
            }
        }
    }

    /**
     * Applies set of cave carving operations to this column to insert the pre-generated cave tunnels from WorldGenerator.
     */
    private void ApplyCaveCarveOperations(ChunkColumn chunkColumn, List<CaveCarveOperation> carveOperations, int regionOriginXVoxels, int regionOriginZVoxels)
    {
        if (carveOperations.Count > 0)
        {
            // size is same as terrain voxels
            foreach (CaveCarveOperation operation in carveOperations)
            {
                // calculate world voxel coordinates for this operation
                int worldX = regionOriginXVoxels + operation.x;
                int worldZ = regionOriginZVoxels + operation.z;
                // air or water voxels are skipped
                VoxelType currentType = chunkColumn.GetVoxel(operation.x, operation.y, operation.z);
                if (currentType != VoxelType.AIR && currentType != VoxelType.WATER)
                {
                    chunkColumn.SetVoxel(worldX, operation.y, worldZ, VoxelType.AIR, normalToTerrainRatio);
                }
            }
        }
    }

    /**
     * Applies snow to this column.
     * Quickly traverses the voxel structure until it finds first non-air chunk. Then it processes the column in small
     * voxel sized sub columns in the XZ plane and if that given XZ position has planned snow placement it finds the actual
     * height at which to place it.
     */
    private void ApplySnow(ChunkColumn chunkColumn)
    {
        // obtain region to get snow data
        var (regionX, regionZ) = GetRegionForChunk(chunkColumn.chunkX, chunkColumn.chunkZ);
        int regionOffsetX = (regionX * metersPerRegion) / metersPerChunk;
        int regionOffsetZ = (regionZ * metersPerRegion) / metersPerChunk;
        
        WorldRegion region = worldGeneratorService.GetRegionWhenReady(regionX, regionZ);
        if (region == null)
        {
            GD.PrintErr($"Cannot apply snow to column {chunkColumn.chunkX},{chunkColumn.chunkZ}, its region {regionX},{regionZ} is not loaded");
            return;
        }
        
        // calculate world voxel coordinates for this chunk column
        int startWorldX = chunkColumn.chunkX * chunkVoxelSize;
        int startWorldZ = chunkColumn.chunkZ * chunkVoxelSize;
        int endWorldX = startWorldX + chunkVoxelSize;
        int endWorldZ = startWorldZ + chunkVoxelSize;
        
        uint firstNonAirChunkY = 0;
                
        // go quickly until first non-air voxel is found
        // note: chunkY should never reach 0 for snow covered columns, so it should be ok
        for (uint y = (uint)(chunkColumn.chunkCountY - 1); y > 0; y--)
        {
            if (!chunkColumn.IsChunkCompletelyAir(y))
            {
                firstNonAirChunkY = y;
                break;
            }
        }

        // iterate through the XZ plane of the chunk column
        for (int worldX = startWorldX; worldX < endWorldX; worldX++)
        {
            for (int worldZ = startWorldZ; worldZ < endWorldZ; worldZ++)
            {
                // calculate the offset within the chunk in voxel units
                int localVoxelX = worldX - startWorldX;
                int localVoxelZ = worldZ - startWorldZ;

                // convert local voxel offset to terrain units
                uint localTerrainX = (uint)(localVoxelX / terrainUnitsVoxelsRatio);
                uint localTerrainZ = (uint)(localVoxelZ / terrainUnitsVoxelsRatio);

                // calculate region-local terrain coordinates
                uint inRegionTerrainX = (uint)((chunkColumn.chunkX - regionOffsetX) * chunkTerrainUnits + localTerrainX);
                uint inRegionTerrainZ = (uint)((chunkColumn.chunkZ - regionOffsetZ) * chunkTerrainUnits + localTerrainZ);
                
                // check if snow should be placed at this position
                if (!region.HasSnowAtLocalCoords((ushort)inRegionTerrainX, (ushort)inRegionTerrainZ))
                    continue;
                
                // find the highest non-air voxel in this sub-column
                uint highestY = 0;
                bool shouldPlace = false;
                
                // obtain the top most voxel coordinate of the first non-air chunk
                uint topStartVoxelY = (uint)((firstNonAirChunkY+1) * chunkVoxelSize -1);
                
                for (uint y = topStartVoxelY; y > 0; y--)
                {
                    VoxelType voxelType = chunkColumn.GetVoxel(worldX, y, worldZ, 1);
                    
                    if (voxelType != VoxelType.AIR)
                    {
                        if (voxelType == VoxelType.WATER)
                        {
                            break;
                        }
                        
                        highestY = y;
                        shouldPlace = true;
                        break;
                    }
                }
                
                // place snow voxel on top of the highest non-air voxel
                if (shouldPlace)
                {
                    chunkColumn.SetVoxel(worldX, highestY + 1, worldZ, VoxelType.SNOW, 1);
                }
            }
        }
    }
}
