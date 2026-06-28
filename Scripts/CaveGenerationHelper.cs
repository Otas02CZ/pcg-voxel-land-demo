// FILE: CaveGenerationHelper.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains helper class for cave generation related tasks

using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Encapsulates parameters needed by the cave generation helper class.
 */
public struct CaveGeneratorHelperParams()
{
    public int seed { get; init; }
    public float thresholdCaves { get; init; }
    public float thresholdTunnelNodes { get; init; }
    public float thresholdEntranceNodes { get; init; }
    
    public ushort caveStartYLevelTerrainUnits { get; init; }
    public ushort caveDeepDarkStoneTerrainUnitsCount { get; init; } = 20;
    public ushort caveDarkStoneTerrainUnitsCount { get; init; } = 20;
    public ushort caveDripstoneTerrainUnitsCount { get; init; } = 20;
    public ushort caveDarkDripstoneTerrainUnitsCount { get; init; } = 20;
}

/**
 * Helper class for cave generation related tasks.
 * Including noise thresholding and cave block types based on vertical level.
 */
public class CaveGenerationHelper
{
    private readonly FastNoiseLite noise; // cave space noise
    // noise thresholds
    private readonly float thresholdCaves;
    private readonly float thresholdTunnelNodes;
    private readonly float thresholdEntranceNodes;
    // height levels of different cave stone voxel types
    private readonly ushort caveDeepDarkStoneTerrainUnitsUpTo;
    private readonly ushort caveDarkStoneTerrainUnitsUpTo;
    private readonly ushort caveDarkDripstoneTerrainUnitsUpTo;
    private readonly ushort caveDripstoneTerrainUnitsUpTo;
    
    
    public CaveGenerationHelper(CaveGeneratorHelperParams parameters)
    {
        thresholdCaves = parameters.thresholdCaves;
        thresholdTunnelNodes = parameters.thresholdTunnelNodes;
        thresholdEntranceNodes = parameters.thresholdEntranceNodes;
        // calculate height levels
        caveDeepDarkStoneTerrainUnitsUpTo = (ushort)(parameters.caveStartYLevelTerrainUnits + parameters.caveDeepDarkStoneTerrainUnitsCount);
        caveDarkStoneTerrainUnitsUpTo = (ushort)(caveDeepDarkStoneTerrainUnitsUpTo + parameters.caveDarkStoneTerrainUnitsCount);
        caveDarkDripstoneTerrainUnitsUpTo = (ushort)(caveDarkStoneTerrainUnitsUpTo + parameters.caveDarkDripstoneTerrainUnitsCount);
        caveDripstoneTerrainUnitsUpTo = (ushort)(caveDarkDripstoneTerrainUnitsUpTo + parameters.caveDripstoneTerrainUnitsCount);
        // configure noise for cave space
        noise = new FastNoiseLite();
        noise.SetSeed(parameters.seed);
        noise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        noise.SetFractalType(FastNoiseLite.FractalTypeEnum.Fbm);
        noise.SetFractalOctaves(5);
        noise.SetFractalLacunarity(1.75f);
        noise.SetFractalGain(0.5f);
        noise.SetFrequency(0.004f);
    }

    /**
     * Returns whether there should be a cave space at given position.
     */
    public bool IsCave(float x, float y, float z)
    {
        float noiseValue = noise.GetNoise3D(x, y, z) * 0.5f + 0.5f;
        return noiseValue > thresholdCaves;
    }
    
    /**
     * Returns whether at given position should be a node of the cave tunnel network system.
     */
    public bool IsTunnelNode(float x, float y, float z)
    {
        float noiseValue = noise.GetNoise3D(x, y, z) * 0.5f + 0.5f;
        return noiseValue > thresholdTunnelNodes;
    }
    
    /**
     * Returns whether at given position should be an entrance node to the cave tunnel network system.
     */
    public bool IsEntranceNode(float x, float y, float z)
    {
        float noiseValue = noise.GetNoise3D(x, y, z) * 0.5f + 0.5f;
        return noiseValue > thresholdEntranceNodes;
    }

    /**
     * Returns stone voxel type for given world height.
     */
    public VoxelType GetCaveTerrainType(uint y)
    {
        if (y < caveDeepDarkStoneTerrainUnitsUpTo)
            return VoxelType.STONE_DARKER;
        if (y < caveDarkStoneTerrainUnitsUpTo)
            return VoxelType.STONE_DARK;
        if (y < caveDarkDripstoneTerrainUnitsUpTo)
            return VoxelType.DRIPSTONE_DARK;
        if (y < caveDripstoneTerrainUnitsUpTo)
            return VoxelType.DRIPSTONE;
        else // regular stone otherwise
            return VoxelType.STONE;
    }
}
