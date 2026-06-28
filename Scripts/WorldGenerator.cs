// FILE: WorldGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains WorldGenerator class that generates world regions in a two-step process.

using System;
using System.Collections.Generic;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Encapsulates parameters for the world generator class.
 */
public struct WorldGeneratorParams()
{
    public int seed { get; init; }
    public int maxTerrainHeightInTerrainUnits { get; init; }
    public int voxelsPerMeter { get; init; }
    public int metersPerRegion { get; init; }
    public int metersPerChunk { get; init; }
    public int unitsPerMeter { get; init; }
    public ushort cavesStartYLevelTerrainUnits { get; init; }
    public bool cavesEnabled { get; init; }
    public CaveGenerationHelper caveGenerationHelper { get; init; }
    public LakeSize lakeSize { get; init; }
    public WaterAvailEffectStrength waterAvailEffectStrength { get; init; }
    public HeightNoiseConfig heightNoiseConfig { get; init; }
    public VegetationChance vegetationChance { get; init; }
    public ushort explicitWaterLevelTerrainUnits { get; init; }
    public int maxRiverLength { get; init; }
    public byte explicitWaterAvailabilityMax { get; init; }
    public float[] areaAgeThresholds { get; init; }
    public float[] areaHeightThresholds { get; init; }
    public byte[] areaWaterAvailabilityThresholds { get; init; }
    public int waterLilyChance { get; init; }
    public int waterReedsChance { get; init; }
    public float snowOccurenceThreshold { get; init; }
}

/**
 * Class for the first step of world generation process in this demo.
 * Generates large sections of the world - WorldRegions. That mostly represent 2D information for the later
 * voxelization process.
 * WorldRegion generation is split into two phases - terrain and features. This is necessary to distribute water availability
 * effects across region borders to neighbors.
 * First phase of WorldRegion generation process includes noise based terrain heightmap generation and
 * water map generation that uses combination of explicitly generated lakes based on low terrain height
 * and WaterGenerator class that uses Priority Flood and simplified gradients to form lakes and rivers.
 * It also includes generation of cave tunnel network that aims to interconnect cave spaces together and carve
 * entrances to the ground. This is the only form of 3D data in WorldRegion that gets voxelized outside VoxelGenerator
 * and is there only applied.
 * Second phase of WorldRegion generation process firstly applies the water availability effects from neighbor regions,
 * then it further increases it by noise to simulate effect of some water availability from underground sources.
 * Then it uses the accumulated information to generate biome map made up from snow placements and ground voxel types and
 * places features made up from vegetation and decoration elements into the world.
 */
public class WorldGenerator
{
    private readonly WorldGeneratorService worldGeneratorService; // service with access to active world regions

    private readonly int seed;
    
    // age classification in range 0 - 1
    private readonly float[] ageThresholds;
    // height thresholds for tree type areas
    private readonly int[] areaHeightThresholds;
    // thresholds of water availability 
    private readonly byte[] areaWaterAvailabilityThresholds;
    
    // cave tunnels configuration
    private readonly bool cavesEnabled;
    private const int maxTunnelDistance = 50;
    private readonly ushort cavesStartYLevel; // from which y level up caves start appearing
    private const ushort caveStartTunnelNodesOffsetTerrainUnits = 12; // offset from the lowest point of caves where tunnel sampling starts
    private const ushort samplingStepTunnelNodesTerrainUnits = 16;
    private const float baseSegmentRadiusMeters = 1f; // radius in meters for tunnel segments
    private const ushort upperLimitTunnelNodesOffsetTerrainUnits = 20; // offset from the terrain height where tunnel sampling ends
    private const ushort aboveGroundTunnelNodesOffsetTerrainUnits = 2; // offset above the terrain height where tunnel entrances are sampled
    private const int regionBoundaryMarginTerrainUnits = 8; // skip nodes closer than this distance to region boundaries
    private const int entranceNodesWaterCheckDistance = 12; // defines circular area around possible tunnel entrance nodes that must be free of water
    // for the tunnel entrance node to succeed
    private const int entranceNodesWaterCheckDistanceSquared = entranceNodesWaterCheckDistance * entranceNodesWaterCheckDistance;
    // scale factor to increase the radius of surface intersection check
    private const float tunnelVoxelizationSurfaceRadiusFactor = 1.5f;
    
    private readonly VegetationChance vegetationChance; // maximums of rng range for vegetation placement chances
    
    // voxel and world sizes configuration
    private readonly int metersPerRegion;
    private readonly int voxelsPerMeter;
    private readonly int metersPerChunk;
    private readonly int regionSizeInTerrainUnits;
    private readonly int minTerrainHeightInTerrainUnits = 2;
    private readonly int maxTerrainHeightInTerrainUnits;
    private readonly float metersPerUnit;
    private readonly int regionSizeInVoxels;
    private readonly int terrainUnitsVoxelsRatio;
    
    private readonly float heightMapContrastExponent = 1.8f; // exponent that scales the final noise value of terrain height
    // higher values increase contrast of valleys to peaks. As everything apart from very high areas gets pushed down a bit.
    
    private readonly int explicitWaterLevelTerrainUnits; // predefined water level in the terrain
    private readonly int explicitWaterAvailabilityMax; // max strength of the underground water noise addition
    private readonly byte maxWaterAvailabilityInfluence; // max influence for the effect of rivers and lakes
    
    private readonly float snowOccurenceThreshold; // in the snow noise map
    private readonly int waterLilyChance; // out of 10000
    private readonly int waterReedsChance; // out of 10000
    private const int waterLilyDistance = 24; // minimum distance in voxels between water lilies
    private const int waterLilyDistanceSquared = waterLilyDistance * waterLilyDistance;
    
    private readonly WaterGeneratorParams waterGeneratorParams;
    // voxel model collections
    private readonly Dictionary<TreeType, Dictionary<TreeSize, Dictionary<TreeState, List<Vegetation>>>> availableTrees;
    private readonly Dictionary<TreeType, List<Vegetation>> availableBushes;
    private readonly List<Vegetation> availableGrasses;
    private readonly List<Vegetation> availablePlants;
    private readonly List<Vegetation> availableReeds;
    private readonly List<Vegetation> availableWaterLilies;
    private readonly List<Rock> availableRocks;
    private readonly List<Trunk> availableTrunks;
    // cave gen helper and noises
    private readonly CaveGenerationHelper caveGenerationHelper;
    private readonly FastNoiseLite mountainMaskNoise; // mask for mixing of ridged and normal terrain height noise
    private readonly FastNoiseLite heightNoise;
    private readonly FastNoiseLite waterAvailabilityNoise;
    private readonly FastNoiseLite ageNoise;
    private readonly FastNoiseLite snowNoise;

    public WorldGenerator(WorldGeneratorParams parameters, WorldGeneratorService worldGeneratorService)
    {
        this.worldGeneratorService = worldGeneratorService;

        seed = parameters.seed;
        metersPerRegion = parameters.metersPerRegion;
        int unitsPerMeter = parameters.unitsPerMeter;
        voxelsPerMeter = parameters.voxelsPerMeter;
        metersPerChunk = parameters.metersPerChunk;
        cavesStartYLevel = parameters.cavesStartYLevelTerrainUnits;
        maxTerrainHeightInTerrainUnits = parameters.maxTerrainHeightInTerrainUnits;
        regionSizeInTerrainUnits = metersPerRegion * unitsPerMeter;
        caveGenerationHelper = parameters.caveGenerationHelper;
        cavesEnabled = parameters.cavesEnabled;
        ageThresholds = parameters.areaAgeThresholds;
        areaHeightThresholds = new int[2]; // lowlands, hills, (mountains)
        areaHeightThresholds[0] = (int)(maxTerrainHeightInTerrainUnits * parameters.areaHeightThresholds[0]);
        areaHeightThresholds[1] = (int)(maxTerrainHeightInTerrainUnits * parameters.areaHeightThresholds[1]);
        areaWaterAvailabilityThresholds = parameters.areaWaterAvailabilityThresholds;
        snowOccurenceThreshold = parameters.snowOccurenceThreshold;
        maxWaterAvailabilityInfluence = parameters.waterAvailEffectStrength.maxWaterInfluence;
        explicitWaterAvailabilityMax = parameters.explicitWaterAvailabilityMax;
        waterLilyChance = parameters.waterLilyChance;
        waterReedsChance = parameters.waterReedsChance;
        vegetationChance = parameters.vegetationChance;
        explicitWaterLevelTerrainUnits = parameters.explicitWaterLevelTerrainUnits;

        waterGeneratorParams = new WaterGeneratorParams()
        {
            prefilledLakeHeightThreshold = explicitWaterLevelTerrainUnits,
            unitsPerMeter = unitsPerMeter,
            regionSizeInUnits = regionSizeInTerrainUnits,
            maxLakeDepth = parameters.lakeSize.maxLakeDepth,
            maxLakeSize = parameters.lakeSize.maxLakeSize,
            maxRiverLength = parameters.maxRiverLength,
            maxWaterInfluenceDistance = parameters.waterAvailEffectStrength.maxWaterInfluenceDistance,
            maxWaterInfluence = parameters.waterAvailEffectStrength.maxWaterInfluence,
            baseInfluence = parameters.waterAvailEffectStrength.baseInfluence
        };

        metersPerUnit = 1f / unitsPerMeter;
        regionSizeInVoxels = metersPerRegion * voxelsPerMeter;
        terrainUnitsVoxelsRatio = voxelsPerMeter / unitsPerMeter;
        
        // prepare collections for models
        availableTrees = new Dictionary<TreeType, Dictionary<TreeSize, Dictionary<TreeState, List<Vegetation>>>>();
        availableTrees[TreeType.CONIFER] = new Dictionary<TreeSize, Dictionary<TreeState, List<Vegetation>>>();
        availableTrees[TreeType.CONIFER][TreeSize.SMALL] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.CONIFER][TreeSize.MEDIUM] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.CONIFER][TreeSize.LARGE] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.CONIFER][TreeSize.SMALL][TreeState.HEALTHY] = [];
        availableTrees[TreeType.CONIFER][TreeSize.SMALL][TreeState.WEAK] = [];
        availableTrees[TreeType.CONIFER][TreeSize.SMALL][TreeState.DEAD] = [];
        availableTrees[TreeType.CONIFER][TreeSize.MEDIUM][TreeState.HEALTHY] = [];
        availableTrees[TreeType.CONIFER][TreeSize.MEDIUM][TreeState.WEAK] = [];
        availableTrees[TreeType.CONIFER][TreeSize.MEDIUM][TreeState.DEAD] = [];
        availableTrees[TreeType.CONIFER][TreeSize.LARGE][TreeState.HEALTHY] = [];
        availableTrees[TreeType.CONIFER][TreeSize.LARGE][TreeState.WEAK] = [];
        availableTrees[TreeType.CONIFER][TreeSize.LARGE][TreeState.DEAD] = [];
        availableTrees[TreeType.DECIDUOUS] = new Dictionary<TreeSize, Dictionary<TreeState, List<Vegetation>>>();
        availableTrees[TreeType.DECIDUOUS][TreeSize.SMALL] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.DECIDUOUS][TreeSize.MEDIUM] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.DECIDUOUS][TreeSize.LARGE] = new Dictionary<TreeState, List<Vegetation>>();
        availableTrees[TreeType.DECIDUOUS][TreeSize.SMALL][TreeState.HEALTHY] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.SMALL][TreeState.WEAK] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.SMALL][TreeState.DEAD] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.MEDIUM][TreeState.HEALTHY] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.MEDIUM][TreeState.WEAK] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.MEDIUM][TreeState.DEAD] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.LARGE][TreeState.HEALTHY] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.LARGE][TreeState.WEAK] = [];
        availableTrees[TreeType.DECIDUOUS][TreeSize.LARGE][TreeState.DEAD] = [];
        
        availableBushes = new Dictionary<TreeType, List<Vegetation>>();
        availableBushes[TreeType.CONIFER] = [];
        availableBushes[TreeType.DECIDUOUS] = [];
        availableGrasses = [];
        availablePlants = [];
        availableReeds = [];
        availableWaterLilies = [];
        availableRocks = [];
        availableTrunks = [];
        
        // setup all noises
        mountainMaskNoise = new FastNoiseLite();
        mountainMaskNoise.SetSeed(seed + 3);
        mountainMaskNoise.SetFrequency(0.001f);
        mountainMaskNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        mountainMaskNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.None);
        
        heightNoise = new FastNoiseLite();
        heightNoise.SetSeed(seed);
        heightNoise.SetFrequency(parameters.heightNoiseConfig.heightNoiseFrequency);
        heightNoise.SetFractalLacunarity(parameters.heightNoiseConfig.heightNoiseLacunarity);
        heightNoise.SetFractalGain(parameters.heightNoiseConfig.heightNoiseFractalGain);
        heightNoise.SetFractalOctaves(parameters.heightNoiseConfig.heightNoiseOctaves);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        heightNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.Fbm);
        
        waterAvailabilityNoise = new FastNoiseLite();
        waterAvailabilityNoise.SetSeed(seed + 10000);
        waterAvailabilityNoise.SetFrequency(0.005f);
        waterAvailabilityNoise.SetFractalLacunarity(2.0f);
        waterAvailabilityNoise.SetFractalGain(0.5f);
        waterAvailabilityNoise.SetFractalOctaves(2);
        waterAvailabilityNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        waterAvailabilityNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.Fbm);
        
        ageNoise = new FastNoiseLite();
        ageNoise.SetSeed(seed + 20000);
        ageNoise.SetFrequency(0.002f);
        ageNoise.SetFractalLacunarity(2.0f);
        ageNoise.SetFractalGain(0.5f);
        ageNoise.SetFractalOctaves(0);
        ageNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        ageNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.None);
        
        snowNoise = new FastNoiseLite();
        snowNoise.SetSeed(seed + 30000);
        snowNoise.SetFrequency(0.01f);
        snowNoise.SetFractalLacunarity(2.0f);
        snowNoise.SetFractalGain(0.5f);
        snowNoise.SetFractalOctaves(0);
        snowNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        snowNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.None);
    }

    /**
     * Sets up supplied models for use in feature placement step.
     */
    public void SetupModels(WorldGeneratorModels worldGenModels)
    {
        List<Vegetation> allVegetationModels = [];
        allVegetationModels.AddRange(worldGenModels.treeBushModels);
        allVegetationModels.AddRange(worldGenModels.grassModels);
        allVegetationModels.AddRange(worldGenModels.plantModels);
        allVegetationModels.AddRange(worldGenModels.reedModels);
        allVegetationModels.AddRange(worldGenModels.waterLilyModels);

        foreach (Vegetation vegetation in allVegetationModels)
        {
            AddAvailableVegetation(vegetation);
        }
		
        foreach (Rock rock in worldGenModels.rockModels)
        {
            availableRocks.Add(rock);
        }
		
        foreach (Trunk trunk in worldGenModels.trunkModels)
        {
            availableTrunks.Add(trunk);
        }
		// models need to be sorted by their stable ids so that placement stays deterministic
        SortModels();
    }

    /**
     * Adds single vegetation model to correct model collection based on its category.
     */
    private void AddAvailableVegetation(Vegetation vegetation)
    {
        switch (vegetation.type)
        {
            case VegetationType.TREE:
                availableTrees[vegetation.treeType][vegetation.size][vegetation.state].Add(vegetation);
                break;
            case VegetationType.BUSH:
                availableBushes[vegetation.treeType].Add(vegetation);
                break;
            case VegetationType.GRASS:
                availableGrasses.Add(vegetation);
                break;
            case VegetationType.PLANT:
                availablePlants.Add(vegetation);
                break;
            case VegetationType.REED:
                availableReeds.Add(vegetation);
                break;
            case VegetationType.WATER_LILLY:
                availableWaterLilies.Add(vegetation);
                break;
        }
    }

    /**
     * Sorts all registered models by their ids to ensure they appear in deterministic order.
     */
    private void SortModels()
    {
        foreach (var treeType in availableTrees)
        {
            foreach (var treeSize in treeType.Value)
            {
                foreach (var treeState in treeSize.Value)
                {
                    treeState.Value.Sort((a, b) => a.id.CompareTo(b.id));
                }
            }
        }

        foreach (var bushType in availableBushes)
        {
            bushType.Value.Sort((a, b) => a.id.CompareTo(b.id));
        }

        availableGrasses.Sort((a, b) => a.id.CompareTo(b.id));
        availablePlants.Sort((a, b) => a.id.CompareTo(b.id));
        availableRocks.Sort((a, b) => a.id.CompareTo(b.id));
        availableTrunks.Sort((a, b) => a.id.CompareTo(b.id));
        availableReeds.Sort((a, b) => a.id.CompareTo(b.id));
        availableWaterLilies.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /**
     * First phase of WorldRegion generation process.
     * Generates heightmap, water map (including water availabilities within the region, and preparation of
     * neighbor region export) and cave tunnel systems, including voxelization into per column sets of carving operations.
     */
    public void GenerateRegionTerrain(WorldRegion region)
    {
        GenerateHeightMap(region);
        GenerateWaterMap(region);
        if (cavesEnabled)
        {
            GenerateCaveSystemTunnels(region);
        }
        
        region.firstStepDone = true;
    }
    
    /**
     * Second phase of WorldRegion generation process.
     * Applies leaked water availabilities from neighbors, adds water availability from underground noise.
     * And generates biomes and feature placement map (vegetation, decoration).
     */
    public void GenerateRegionFeatures(WorldRegion region)
    {
        ApplyEffectsOfNeighboringWater(region);
        GenerateNoiseBasedWaterAvailabilityMap(region);
        
        GenerateBiomeMap(region);
        GeneratePlantDecorationMap(region);
        
        region.ready = true;
    }
    
    /**
     * Applies water availability effects that cross boundaries of 8 neighboring regions into this one.
     */
    private void ApplyEffectsOfNeighboringWater(WorldRegion region)
    {
        // iterate through all 8 neighbors
        for (short neighborX = -1; neighborX <= 1; neighborX++)
        {
            for (short neighborZ = -1; neighborZ <= 1; neighborZ++)
            {
                // skip self
                if (neighborX == 0 && neighborZ == 0)
                    continue;
                
                // calculate neighbor region coordinates
                int neighborRegionX = region.posX + neighborX;
                int neighborRegionZ = region.posZ + neighborZ;
                
                // try to get neighbor region, should be ready
                WorldRegion neighborRegion = worldGeneratorService.GetRegion(neighborRegionX, neighborRegionZ);
                if (neighborRegion == null)
                    continue; // skip
                
                // check neighbor region completed terrain generation step
                if (!neighborRegion.firstStepDone)
                    continue;
                
                // get water effects that the neighbor exported to this region
                short oppositeNx = (short)-neighborX;
                short oppositeNz = (short)-neighborZ;
                
                WaterEffect[] effects = neighborRegion.GetNeighborWaterEffects(oppositeNx, oppositeNz);
                if (effects == null || effects.Length <= 0)
                    continue;
                
                // apply each water effect to this region
                foreach (WaterEffect effect in effects)
                {
                    // already in the same coordinate space
                    ushort localX = effect.posX;
                    ushort localZ = effect.posZ;
                    
                    // combine with existing value
                    byte currentValue = region.GetWaterAvailabilityAtLocalCoords(localX, localZ);
                    byte newValue = (byte)(currentValue + effect.value);
                    // cap at maximum total influence
                    newValue = Math.Min(newValue, maxWaterAvailabilityInfluence);
                    // set the new water availability value
                    region.SetWaterAvailability(localX, localZ, newValue);
                }
            }
        }
    }

    /**
     * Generates height map in the given region.
     * Uses two noises. Height noise to obtain base height value, for which it computes
     * ridged noise value as (1 - abs(noise))^2.
     * The final noise value is then formed as a mix of base height and its ridged value.
     * Where the strength of each component is defined by the second noise - mountain mask noise.
     * Then the value is normalized and transformed once more by final^exp which increases distribution
     * of lower areas and overall contrast.
     * Finally it is used to compute actual height in terrain units.
     */
    private void GenerateHeightMap(WorldRegion region)
    {
        int regionOriginX = region.posX * metersPerRegion;
        int regionOriginZ = region.posZ * metersPerRegion;

        // for each terrain unit spot
        for (ushort x = 0; x < regionSizeInTerrainUnits; x++)
        {
            float xScaled = x * metersPerUnit + regionOriginX;
            for (ushort z = 0; z < regionSizeInTerrainUnits; z++)
            {
                float zScaled = z * metersPerUnit + regionOriginZ;
                // get regular height noise value
                float noiseValue = heightNoise.GetNoise2D(xScaled, zScaled);
                // get mountain mask noise value and scale it to 0 - 1
                float mountainMaskValue = mountainMaskNoise.GetNoise2D(xScaled, zScaled);
                mountainMaskValue = mountainMaskValue * 0.5f + 0.5f;
                
                // compute ridged noise value
                float ridgedValue = 1f - Math.Abs(noiseValue);
                ridgedValue *= ridgedValue;
                
                // blend ridged and regular noise based on mountain mask
                float finalValue = ridgedValue * mountainMaskValue + (1f - mountainMaskValue) * noiseValue;
                
                // normalize to 0 - 1 range
                float normalizedValue = finalValue * 0.5f + 0.5f;
                
                // apply contrast and distribution exponent
                float scaledValue = (float)Math.Pow(normalizedValue, heightMapContrastExponent);
                
                // scale within minHeightInTerrainUnits and maxHeightInTerrainUnits
                ushort yLevel = (ushort)(minTerrainHeightInTerrainUnits + scaledValue * (maxTerrainHeightInTerrainUnits - minTerrainHeightInTerrainUnits));
                region.SetHeight(x, z, yLevel);
            }
        }
    }

    /**
     * Generates cave tunnel network system by rough sampling of the cave noise.
     * This way a set of underground tunnel nodes is found as well as a small amount
     * of entrance nodes right at the terrain height level. These nodes are then interconnected
     * into tunnel segments which are pre-voxelized and sliced into region columns so that each
     * column has a set of carving operations which will be applied in the VoxelGenerator.
     */
    private void GenerateCaveSystemTunnels(WorldRegion region)
    {
        int startX = region.posX * regionSizeInTerrainUnits;
        int startZ = region.posZ * regionSizeInTerrainUnits;
        int endX = startX + regionSizeInTerrainUnits;
        int endZ = startZ + regionSizeInTerrainUnits;

        List<Vector3Int> caveTunnelNodes = [];
        List<Vector3Int> entranceNodes = [];
        
        // loop over XZ space of region
        for (int x = startX; x < endX; x += samplingStepTunnelNodesTerrainUnits)
        {
            // skip if outside of region boundary margin
            if (x - startX < regionBoundaryMarginTerrainUnits || endX - x <= regionBoundaryMarginTerrainUnits)
                continue;
            
            // calculate world X coordinate for sampling
            int actualX = x * terrainUnitsVoxelsRatio;
            for (int z = startZ; z < endZ; z += samplingStepTunnelNodesTerrainUnits)
            {
                // skip if outside of region boundary margin
                if (z - startZ < regionBoundaryMarginTerrainUnits || endZ - z <= regionBoundaryMarginTerrainUnits)
                    continue;
                
                // calculate world Z coordinate for sampling
                int actualZ = z * terrainUnitsVoxelsRatio;

                // lower sampling boundary
                int lowerLimitWorldY = cavesStartYLevel + caveStartTunnelNodesOffsetTerrainUnits;
                // calculate x, z region local coordinates for height lookup
                ushort localX = (ushort)(x - startX);
                ushort localZ = (ushort)(z - startZ);
                ushort terrainHeightAtPosTerrainUnits = region.GetHeightAtLocalCoords(localX, localZ);
                // upper sampling boundary
                int upperLimitWorldY = terrainHeightAtPosTerrainUnits - upperLimitTunnelNodesOffsetTerrainUnits;
                
                if (upperLimitWorldY <= lowerLimitWorldY)
                    continue; // skip
                // sample the noise in this sampling column
                for (int y = lowerLimitWorldY; y < upperLimitWorldY; y += samplingStepTunnelNodesTerrainUnits)
                {
                    int actualY = y * terrainUnitsVoxelsRatio;
                    if (caveGenerationHelper.IsTunnelNode(actualX, actualY, actualZ))
                    {
                        caveTunnelNodes.Add(new Vector3Int(x, y, z));
                    }
                }

                // check potential entrance, only a single position for each sampling column
                uint aboveGroundWorldY = (uint)((terrainHeightAtPosTerrainUnits + aboveGroundTunnelNodesOffsetTerrainUnits));
                int actualAboveGroundWorldY = (int)aboveGroundWorldY * terrainUnitsVoxelsRatio;
                // sample the spot
                if (caveGenerationHelper.IsEntranceNode(actualX, actualAboveGroundWorldY, actualZ))
                {
                    // skip entrances if these are too close to water features
                    bool hasWaterNearby = false;
                    // check in circular area around the spot
                    ushort checkMinX = (ushort)Math.Max(0, localX - entranceNodesWaterCheckDistance);
                    ushort checkMaxX = (ushort)Math.Min(regionSizeInTerrainUnits - 1, localX + entranceNodesWaterCheckDistance);
                    ushort checkMinZ = (ushort)Math.Max(0, localZ - entranceNodesWaterCheckDistance);
                    ushort checkMaxZ = (ushort)Math.Min(regionSizeInTerrainUnits - 1, localZ + entranceNodesWaterCheckDistance);
                    for (ushort checkX = checkMinX; checkX <= checkMaxX && !hasWaterNearby; checkX++)
                    {
                        ushort distanceX = (ushort)(checkX - localX);
                        int distanceXSquared = distanceX * distanceX;
                        for (ushort checkZ = checkMinZ; checkZ <= checkMaxZ && !hasWaterNearby; checkZ++)
                        {
                            ushort distanceZ = (ushort)(checkZ - localZ);
                            int distanceZSquared = distanceZ * distanceZ;
                            // circle check
                            if (distanceXSquared + distanceZSquared > entranceNodesWaterCheckDistanceSquared)
                                continue;
                            if (region.GetWaterLevelAtLocalCoords(checkX, checkZ) != 0)
                            {
                                hasWaterNearby = true;
                            }
                        }
                    }
                    if (!hasWaterNearby)
                    {
                        entranceNodes.Add(new Vector3Int(x, (int)aboveGroundWorldY, z));
                    }
                }
            }
        }
        
        if (caveTunnelNodes.Count == 0)
            return;
        
        // create graph, connect each node to its closest neighbor within the region
        List<CaveTunnelSegment> caveTunnelSegments = [];
        foreach (Vector3Int node in caveTunnelNodes)
        {
            Vector3Int? closestNode = null;
            float closestDistance = float.MaxValue;
            foreach (Vector3Int otherNode in caveTunnelNodes)
            {
                if (otherNode.Equals(node))
                    continue;
                
                float distance = node.DistanceTo(otherNode);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = otherNode;
                }
            }
            if (closestNode.HasValue)
            {
                caveTunnelSegments.Add(new CaveTunnelSegment(node, closestNode.Value, baseSegmentRadiusMeters));
            }
        }
        
        // entrances - connect above ground nodes to closest tunnel node
        foreach (Vector3Int entranceNode in entranceNodes)
        {
            Vector3Int? closestTunnelNode = null;
            float closestDistance = float.MaxValue;
            foreach (Vector3Int tunnelNode in caveTunnelNodes)
            {
                float distance = entranceNode.DistanceTo(tunnelNode);
                if (distance > maxTunnelDistance) // skip if too far
                    continue;
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTunnelNode = tunnelNode;
                }
            }

            if (closestTunnelNode.HasValue)
            {
                caveTunnelSegments.Add(new CaveTunnelSegment(entranceNode, closestTunnelNode.Value, baseSegmentRadiusMeters));
            }
        }
        
        // pre-voxelize cave tunnels and mark entrances with surface intersections to disable feature placement there
        VoxelizeCaveTunnelsAndMarkSurface(caveTunnelSegments, region);
    }
    
    /**
     * Combines voxelization of cave tunnels and marking of surface with entrance intersections as disabled space for feature placement.
     * Segments are voxelized by moving spheres of predefined radius along line from starting to end position of the segment.
     */
    private void VoxelizeCaveTunnelsAndMarkSurface(List<CaveTunnelSegment> segments, WorldRegion region)
    {
        int colSizeVoxels = metersPerChunk * voxelsPerMeter;
        int carveSize = terrainUnitsVoxelsRatio; // one terrain unit in voxels

        int regionVoxelOriginX = region.posX * regionSizeInVoxels;
        int regionVoxelOriginZ = region.posZ * regionSizeInVoxels;
        int regionVoxelEndX = regionVoxelOriginX + regionSizeInVoxels;
        int regionVoxelEndZ = regionVoxelOriginZ + regionSizeInVoxels;

        // used for carve deduplication
        HashSet<(int, int, int)> visited = [];

        // process each segment of tunnel network
        foreach (CaveTunnelSegment seg in segments)
        {
            // convert segment start and end points from world terrain units to world voxel coordinates
            float startX = seg.start.x * terrainUnitsVoxelsRatio;
            float startY = seg.start.y * terrainUnitsVoxelsRatio;
            float startZ = seg.start.z * terrainUnitsVoxelsRatio;
            float endX = seg.end.x * terrainUnitsVoxelsRatio;
            float endY = seg.end.y * terrainUnitsVoxelsRatio;
            float endZ = seg.end.z * terrainUnitsVoxelsRatio;

            // direction vector of the segment in voxel space
            float dX = endX - startX;
            float dY = endY - startY;
            float dZ = endZ - startZ;

            // carve radius and surface-check radius in voxels
            float carveRadiusVoxel = seg.radius * voxelsPerMeter;
            float carveRadiusVoxelSquared = carveRadiusVoxel * carveRadiusVoxel;
            float surfaceRadiusVoxel = carveRadiusVoxel * tunnelVoxelizationSurfaceRadiusFactor; // surface check has larger radius to clear more space
            float surfaceRadiusVoxelSquared = surfaceRadiusVoxel * surfaceRadiusVoxel;

            // discard near zero length segments
            float segmentLengthVoxel = (float)Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
            if (segmentLengthVoxel < 0.001f)
                continue;

            // calculate number of steps
            int steps = Math.Max(1, (int)Math.Ceiling(segmentLengthVoxel / carveSize)); // voxelization steps along the line
            int carveRadiusSteps = (int)Math.Ceiling(carveRadiusVoxel / carveSize);
            int surfaceRadiusSteps  = (int)Math.Ceiling(surfaceRadiusVoxel / carveSize);

            // run segment voxelization by moving spheres along segment defined line
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;

                // sphere center for voxelization at this step
                int sphereX = (int)Math.Round(startX + dX * t);
                int sphereY = (int)Math.Round(startY + dY * t);
                int sphereZ = (int)Math.Round(startZ + dZ * t);

                int centerX = (sphereX / carveSize) * carveSize;
                int centerY = (sphereY / carveSize) * carveSize;
                int centerZ = (sphereZ / carveSize) * carveSize;

                // looping in X dimension
                for (int pX = -surfaceRadiusSteps; pX <= surfaceRadiusSteps; pX++)
                {
                    float carveX = pX * carveSize;
                    float carveXSquared = carveX * carveX;
                    // outside sphere
                    if (carveXSquared > surfaceRadiusVoxelSquared)
                        continue;

                    int finalX = centerX + pX * carveSize;

                    // check region X bounds
                    if (finalX < regionVoxelOriginX || finalX >= regionVoxelEndX)
                        continue;

                    // looping in Z dimension
                    for (int pZ = -surfaceRadiusSteps; pZ <= surfaceRadiusSteps; pZ++)
                    {
                        float carveZ = pZ * carveSize;
                        float xzDistanceSquared = carveXSquared + carveZ * carveZ;
                        // outside sphere
                        if (xzDistanceSquared > surfaceRadiusVoxelSquared)
                            continue;

                        int finalZ = centerZ + pZ * carveSize;

                        // check region Z bounds
                        if (finalZ < regionVoxelOriginZ || finalZ >= regionVoxelEndZ)
                            continue;

                        // calculate local voxel coordinates within the region
                        int localX = finalX - regionVoxelOriginX;
                        int localZ = finalZ - regionVoxelOriginZ;
                        ushort lx = (ushort)(localX / carveSize);
                        ushort lz = (ushort)(localZ / carveSize);

                        // surface Y in voxel space at this XZ position
                        float surfaceVoxelY = region.GetHeightAtLocalCoords(lx, lz) * carveSize;

                        // surface intersection check
                        float offYSurf = surfaceVoxelY - centerY;
                        if (xzDistanceSquared + offYSurf * offYSurf <= surfaceRadiusVoxelSquared)
                        {
                            region.SetPositionDisabled(lx, lz);
                        }

                        // discard if outside the carving sphere (surface sphere is larger than carving sphere)
                        if (xzDistanceSquared > carveRadiusVoxelSquared)
                            continue;

                        // looping in Y dimension
                        for (int pY = -carveRadiusSteps; pY <= carveRadiusSteps; pY++)
                        {
                            float carveY = pY * carveSize;
                            // outside carving sphere
                            if (xzDistanceSquared + carveY * carveY > carveRadiusVoxelSquared)
                                continue;

                            int finalY = centerY + pY * carveSize;
                            if (finalY < 0)
                                continue;

                            // deduplication check
                            if (!visited.Add((finalX, finalY, finalZ)))
                                continue;
                            
                            // calculate column coordinates and save the voxel operation
                            ushort colX = (ushort)(localX / colSizeVoxels);
                            ushort colZ = (ushort)(localZ / colSizeVoxels);
                            region.AddCaveCarveOperation(new CaveCarveOperation((ushort)localX, (ushort)finalY, (ushort)localZ), colX, colZ);
                        }
                    }
                }
            }
        }
    }
    
    /**
     * Adds influence of noise based water availability to the region.
     */
    private void GenerateNoiseBasedWaterAvailabilityMap(WorldRegion region)
    {
        int regionOriginX = region.posX * metersPerRegion;
        int regionOriginZ = region.posZ * metersPerRegion;
        // loop through region terrain units
        for (ushort x = 0; x < regionSizeInTerrainUnits; x++)
        {
            float xScaled = x * metersPerUnit;
            for (ushort z = 0; z < regionSizeInTerrainUnits; z++)
            {
                float zScaled = z * metersPerUnit;
                // obtain water availability noise value and scale it
                float noiseValue = waterAvailabilityNoise.GetNoise2D(xScaled + regionOriginX, zScaled + regionOriginZ);
                byte noiseByte = (byte)((noiseValue * 0.5f + 0.5f) * explicitWaterAvailabilityMax); // scale to 0 - max
                // modify current value of water availability
                byte currentValue = region.GetWaterAvailabilityAtLocalCoords(x, z);
                int finalValue = currentValue + noiseByte;
                finalValue = Math.Clamp(finalValue, 0, 255);
                region.SetWaterAvailability(x, z, (byte)finalValue);
            }
        }
    }
    
    /**
     * Generates water map.
     * Firstly fills lower areas of terrain with water up to a defined water level.
     * Then uses WaterGenerator to generate lakes and river systems.
     * Together with their water availability effects.
     */
    private void GenerateWaterMap(WorldRegion region)
    {
        // fill spots below the water level threshold with water
        for (ushort x = 0; x < regionSizeInTerrainUnits; x++)
        {
            for (ushort z = 0; z < regionSizeInTerrainUnits; z++)
            {
                ushort heightAtPos = region.GetHeightAtLocalCoords(x, z);
                if (heightAtPos < explicitWaterLevelTerrainUnits)
                {
                    region.SetWaterLevel(x, z, (short)(explicitWaterLevelTerrainUnits - heightAtPos));
                }
            }
        }
        
        // generate water network and its effects on water availability for this region
        WaterGenerator waterGen = new WaterGenerator(region, waterGeneratorParams);
        waterGen.GenerateWaterSystem();
    }
    
    /**
     * Generates biome map from terrain height and water availability information.
     * Biome map is represented by snow placement and types of ground voxels.
     */
    private void GenerateBiomeMap(WorldRegion region)
    {
        int regionOriginX = region.posX * metersPerRegion;
        int regionOriginZ = region.posZ * metersPerRegion;

        for (ushort x = 0; x < regionSizeInTerrainUnits; x++)
        {
            float xScaled = x * metersPerUnit;
            for (ushort z = 0; z < regionSizeInTerrainUnits; z++)
            {
                // height
                float zScaled = z * metersPerUnit;
                ushort heightAtPos = region.GetHeightAtLocalCoords(x, z);
                
                // lowland areas
                if (heightAtPos < areaHeightThresholds[0])
                {
                    // water
                    byte waterAvailability = region.GetWaterAvailabilityAtLocalCoords(x, z);
                    if (waterAvailability < areaWaterAvailabilityThresholds[0])
                    {
                        // arid
                        region.SetGroundVoxelType(x, z, VoxelType.SAND);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[1])
                    {
                        // low moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[2])
                    {
                        // medium moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_DARK);
                    }
                    else
                    {
                        // high moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_DARKER);
                    }
                }
                // hilly areas
                else if (heightAtPos < areaHeightThresholds[1])
                {
                    // water
                    byte waterAvailability = region.GetWaterAvailabilityAtLocalCoords(x, z);
                    if (waterAvailability < areaWaterAvailabilityThresholds[0])
                    {
                        // arid
                        region.SetGroundVoxelType(x, z, VoxelType.SAND);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[1])
                    {
                        // low moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[2])
                    {
                        // medium moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_DARK);
                    }
                    else
                    {
                        // high moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_DARKER);
                    }
                }
                // mountains
                else
                {
                    // snow can be here
                    float snowNoiseValue = (float)(snowNoise.GetNoise2D(xScaled + regionOriginX, zScaled + regionOriginZ) * 0.5 + 0.5f); // scale to 0 - 1
                    if (snowNoiseValue >= snowOccurenceThreshold)
                    {
                        region.SetSnowAtLocalCoords(x, z);
                    }
                    
                    // water
                    byte waterAvailability = region.GetWaterAvailabilityAtLocalCoords(x, z);
                    if (waterAvailability < areaWaterAvailabilityThresholds[0])
                    {
                        // arid
                        region.SetGroundVoxelType(x, z, VoxelType.STONE);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[1])
                    {
                        // low moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_BROWN_YELLOW);
                    }
                    else if (waterAvailability < areaWaterAvailabilityThresholds[2])
                    {
                        // medium moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_BROWN);
                    }
                    else
                    {
                        // high moisture
                        region.SetGroundVoxelType(x, z, VoxelType.GROUND_GRASS_BROWN_DARK);
                    }
                }
            }
        }
    }

    /**
     * Generates placement map of features such as vegetation and decorations.
     * Uses terrain height, water availability and age noise as input information.
     * Placement is randomized, with the above mentioned parameters acting as weights for
     * placement odds of various types of features.
     */
    private void GeneratePlantDecorationMap(WorldRegion region)
    {
        int regionOriginX = region.posX * metersPerRegion;
        int regionOriginZ = region.posZ * metersPerRegion;
        // random number generator with seed offset for this region
        Random random = new Random(seed + region.posX * metersPerRegion + region.posZ);
        
        // already placed water lilies in the region to track spacing between them
        HashSet<(ushort x, ushort z)> waterLilyPositions = new();
        
        // process XZ plane in voxel sized units
        for (uint x = 0; x < regionSizeInVoxels; x++)
        {
            ushort xTerrainUnits = (ushort)(x / terrainUnitsVoxelsRatio);
            ushort colX = (ushort)Math.Floor((double)x / (metersPerChunk * voxelsPerMeter));
            for (uint z = 0; z < regionSizeInVoxels; z++)
            {
                ushort zTerrainUnits = (ushort)(z / terrainUnitsVoxelsRatio);
                
                // check whether position is not disabled due to cave entrances
                if (region.IsPositionDisabled(xTerrainUnits, zTerrainUnits))
                    continue;
                
                ushort colZ = (ushort)Math.Floor((double)z / (metersPerChunk * voxelsPerMeter));
                // obtain parameters
                short waterLevel = region.GetWaterLevelAtLocalCoords(xTerrainUnits, zTerrainUnits);
                ushort heightAtPos = region.GetHeightAtLocalCoords(xTerrainUnits, zTerrainUnits);
                bool hasSnow = region.HasSnowAtLocalCoords(xTerrainUnits, zTerrainUnits);
                
                // placement of water plants
                if (waterLevel != 0 && !hasSnow) // only in water and outside snowy areas
                {
                    int randValueWater = random.Next(10000); // random number for reed / water lilies
                    
                    if (waterLevel == 1) //place reeds in shallow water
                    {
                        if (randValueWater < waterReedsChance)
                        {
                            if (availableReeds.Count == 0)
                                continue;
                            int reedIndex = random.Next(availableReeds.Count);
                            Vegetation reedToPlace = availableReeds[reedIndex];
                            region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, reedToPlace), colX, colZ);
                        }

                        continue;
                    }
                    if (waterLevel is > 1 and < 14) // place water lilies outside shallow and very deep areas
                    {
                        if (randValueWater < waterLilyChance)
                        {
                            if (availableWaterLilies.Count == 0)
                                continue;
                                
                            // check distance to other lily plants
                            bool skipPlacement = false;
                            foreach (var position in waterLilyPositions)
                            {
                                int dx = position.x - (int)x;
                                int dz = position.z - (int)z;
                                if (dx * dx + dz * dz < waterLilyDistanceSquared) // circle
                                {
                                    skipPlacement = true;
                                    break;
                                }
                            }
                            if (skipPlacement)
                                continue;
                                
                            int waterLillyIndex = random.Next(availableWaterLilies.Count);
                            // needs to be placed at water level height with small offset, so that waves can come across it
                            int waterLillyOffsetY = waterLevel * terrainUnitsVoxelsRatio - 2; // convert water level in terrain units to voxels
                            Vegetation waterLillyToPlace = availableWaterLilies[waterLillyIndex];
                            region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, waterLillyToPlace, (short)waterLillyOffsetY), colX, colZ);
                            waterLilyPositions.Add(((ushort)x, (ushort)z));
                        }
                    }
                    continue;
                }
                // placement of features in non water areas
                // firstly obtain and prepare parameters for this spot
                
                byte waterAvailability = region.GetWaterAvailabilityAtLocalCoords(xTerrainUnits, zTerrainUnits);

                bool canBeDeadTree = false;
                int chanceConifer = 0;
                int chanceDeciduous = 0;
                // terrain height effect step
                // terrain height effects types of trees and whether a dead tree can spawn
                if (heightAtPos < areaHeightThresholds[0])
                {
                    // lowland, only deciduous trees, can spawn dead trees
                    chanceDeciduous = 100;
                    canBeDeadTree = true;
                }
                else if (heightAtPos < areaHeightThresholds[1])
                {
                    // hills, conifer and deciduous trees, can spawn dead trees
                    chanceConifer = 50;
                    chanceDeciduous = 50;
                    canBeDeadTree = true;
                }
                else
                {
                    // mountains, only conifer trees
                    chanceConifer = 100;
                    // dead trees can not be here as deserts are turned into rocky areas without any vegetation whatsoever
                }
                
                // water availability effect step
                // effects overall vegetation amount, health state of trees
                int vegetationRandomMax = 15000;
                TreeState treeState = TreeState.HEALTHY;
                bool noPlants = false;
                if (waterAvailability < areaWaterAvailabilityThresholds[0])
                {
                    // arid, low chance of vegetation and only dead trees
                    vegetationRandomMax = vegetationChance.vegetationRandomMaxArid;
                    treeState = TreeState.DEAD;
                }
                else if (waterAvailability < areaWaterAvailabilityThresholds[1])
                {
                    // low moisture, low chance of vegetation and only weak trees
                    vegetationRandomMax = vegetationChance.vegetationRandomMaxLowWater;
                    treeState = TreeState.WEAK;
                    noPlants = true;
                }
                else if (waterAvailability < areaWaterAvailabilityThresholds[2])
                {
                    // medium moisture, medium chance of vegetation and 75% healthy, 25% weak trees
                    vegetationRandomMax = vegetationChance.vegetationRandomMaxMediumWater;
                    if (random.Next(100) < 25)
                        treeState = TreeState.WEAK;
                } 
                else
                {
                    // high moisture, higher chance of vegetation and only healthy trees
                    vegetationRandomMax = vegetationChance.vegetationRandomMaxHighWater;
                }

                // chances for vegetation types and decoration
                int treeChance = 4;
                int bushChance = 2;
                int grassChance = 174;
                int plantChance = 39;
                int rockChance = 2;
                int trunkChance = 4;
                // chances for tree size types
                int largeTreeChance = 0;
                int mediumTreeChance = 0;
                //int smallTreeChance = 0;
                // areas age effect
                // young areas - only small vegetation - grass, plants, no trees
                // old areas - all types of vegetation, high chance of trees and especially large ones
                float ageNoiseValue = ageNoise.GetNoise2D(x + regionOriginX, z + regionOriginZ) * 0.5f + 0.5f; // scale to 0 - 1
                if (ageNoiseValue < ageThresholds[0])
                {
                    // very young areas, mostly grass and some plants, no trees and bushes
                    treeChance = 0;
                    bushChance = 0;
                    grassChance = 185;
                    plantChance = 15;
                    trunkChance = 0;
                } else if (ageNoiseValue < ageThresholds[1])
                {
                    // young areas, only grass and plants, still no trees and bushes, but more plants
                    treeChance = 0;
                    bushChance = 0;
                    grassChance = 165;
                    plantChance = 36;
                    trunkChance = 1;
                } else if (ageNoiseValue < ageThresholds[2])
                {
                    // young-medium areas, grass, plants and small chance for trees and bushes, only small trees
                    treeChance = 1;
                    bushChance = 1;
                    grassChance = 147;
                    plantChance = 33;
                    trunkChance = 1;
                    //smallTreeChance = 100;
                } else if (ageNoiseValue < ageThresholds[3])
                {
                    // medium areas, grass and plants, higher chance for trees and bushes, small and medium trees
                    treeChance = 3;
                    bushChance = 2;
                    grassChance = 145;
                    plantChance = 28;
                    trunkChance = 1;
                    //smallTreeChance = 70;
                    mediumTreeChance = 30;
                } else if (ageNoiseValue < ageThresholds[4])
                {
                    // medium-old areas, grass and plants, high chance for trees and bushes, small medium and some large trees
                    treeChance = 4;
                    bushChance = 2;
                    grassChance = 130;
                    plantChance = 25;
                    trunkChance = 2;
                    //smallTreeChance = 35;
                    mediumTreeChance = 65;
                    largeTreeChance = 5;
                } else if (ageNoiseValue < ageThresholds[5])
                {
                    // old areas, grass and plants, very high chance for trees and bushes, all tree sizes
                    treeChance = 5;
                    bushChance = 3;
                    grassChance = 115;
                    plantChance = 20;
                    trunkChance = 3;
                    //smallTreeChance = 20;
                    mediumTreeChance = 50;
                    largeTreeChance = 30;
                } else if (ageNoiseValue < ageThresholds[6])
                {
                    // very old areas, grass and plants, extremely high chance for trees and bushes, all tree sizes with higher chance for large trees
                    treeChance = 5;
                    bushChance = 3;
                    grassChance = 90;
                    plantChance = 15;
                    trunkChance = 4;
                    //smallTreeChance = 5;
                    mediumTreeChance = 40;
                    largeTreeChance = 55;
                } else
                {
                    // ancient areas, grass and plants, almost only trees and bushes, only medium and large trees
                    treeChance = 6;
                    bushChance = 4;
                    grassChance = 50;
                    plantChance = 10;
                    trunkChance = 4;
                    //smallTreeChance = 0;
                    mediumTreeChance = 10;
                    largeTreeChance = 90;
                }
                
                // actual placement code that uses the parameters assembled above
                
                int randValue = random.Next(vegetationRandomMax); // random value for vegetation and decoration placement
                
                // decide what to place
                if (randValue < treeChance)
                {
                    // place a tree
                    if (availableTrees.Count == 0)
                        continue;
                    
                    // only place dead trees when allowed
                    if (treeState == TreeState.DEAD && !canBeDeadTree)
                        continue;
                    
                    // tree size roll
                    int roll = random.Next(100);
                    TreeSize treeSize = TreeSize.SMALL;
                    if (roll < largeTreeChance)
                        treeSize = TreeSize.LARGE;
                    else if (roll < largeTreeChance + mediumTreeChance)
                        treeSize = TreeSize.MEDIUM;
                    
                    // tree type roll
                    TreeType treeType = TreeType.CONIFER;
                    roll = random.Next(100);
                    if (roll < chanceConifer)
                        treeType = TreeType.CONIFER;
                    else if (roll < chanceConifer + chanceDeciduous)
                        treeType = TreeType.DECIDUOUS;
                    else
                        continue; // should not happen
                    
                    List<Vegetation> treeList = availableTrees[treeType][treeSize][treeState];
                    if (treeList.Count == 0)
                        continue;
                    // roll for tree selection
                    int treeIndex = random.Next(treeList.Count);
                    Vegetation treeToPlace = treeList[treeIndex];
                    // place the tree
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, treeToPlace), colX, colZ);
                }
                else if (randValue < treeChance + bushChance)
                {
                    // place a bush
                    if (availableBushes.Count == 0)
                        continue;
                    
                    // do not place bushes in arid areas
                    if (treeState == TreeState.DEAD)
                        continue;
                    
                    // bush type roll
                    TreeType bushType = TreeType.CONIFER;
                    int roll = random.Next(100);
                    if (roll < chanceConifer)
                        bushType = TreeType.CONIFER;
                    else if (roll < chanceConifer + chanceDeciduous)
                        bushType = TreeType.DECIDUOUS;
                    else
                        continue; // should not happen
                    
                    List<Vegetation> bushList = availableBushes[bushType];
                    if (bushList.Count == 0)
                        continue;
                    // roll for bush selection
                    int bushIndex = random.Next(bushList.Count);
                    Vegetation bushToPlace = bushList[bushIndex];
                    // place the bush
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, bushToPlace), colX, colZ);
                }
                else if (randValue < treeChance + bushChance + grassChance)
                {
                    // place grass
                    if (hasSnow || availableGrasses.Count == 0)
                        continue;
                    
                    // do not place grass in arid areas
                    if (treeState == TreeState.DEAD)
                        continue;
                    
                    int grassIndex = random.Next(availableGrasses.Count);
                    Vegetation grassToPlace = availableGrasses[grassIndex];
                    // place the grass
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, grassToPlace), colX, colZ);
                }
                else if (randValue < treeChance + bushChance + grassChance + plantChance)
                {
                    // place plant
                    if (hasSnow || availablePlants.Count == 0)
                        continue;
                    
                    // do not place plants in arid areas or areas with low moisture
                    if (treeState == TreeState.DEAD || noPlants)
                        continue;
                    
                    int plantIndex = random.Next(availablePlants.Count);
                    Vegetation plantToPlace = availablePlants[plantIndex];
                    // place the plant
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, plantToPlace), colX, colZ);
                }
                else if (randValue < treeChance + bushChance + grassChance + plantChance + rockChance)
                {
                    // place rock
                    if (availableRocks.Count == 0)
                        continue;
                    
                    int rockIndex = random.Next(availableRocks.Count);
                    Rock rockToPlace = availableRocks[rockIndex];
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, rockToPlace), colX, colZ);
                }
                else if (randValue < treeChance + bushChance + grassChance + plantChance + rockChance + trunkChance)
                {
                    // place trunk
                    if (availableTrunks.Count == 0)
                        continue;

                    int trunkIndex = random.Next(availableTrunks.Count);
                    Trunk trunkToPlace = availableTrunks[trunkIndex];
                    region.AddFeature(new FeaturePlacement((ushort)x, (ushort)z, trunkToPlace), colX, colZ);
                }
            }
        }
    }
}
