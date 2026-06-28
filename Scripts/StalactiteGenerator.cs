// FILE: StalactiteGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains a simple StalactiteGenerator for generation of stalactites and stalagmites

using System;
using System.Collections.Generic;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Stalactite DOWN / stalagmite UP
 */
public enum STALACTITE_GROWTH_DIR : byte
{
    UP,
    DOWN,
}

/**
 * Structure holding parameters for StalactiteGenerator
 */
public struct StalactiteDefinition(
    ushort minLength,
    ushort maxLength,
    ushort radius,
    ushort lengthDecrease, // spike max length decrease factor with distance from stalactite center
    float minLengthRatio, // ration of spike min length against its max length
    STALACTITE_GROWTH_DIR growthDir,
    VoxelType voxelType,
    int id = 0,
    int seed = 5000
    )
{
    public int seed = seed;
    public int id = id;
    public readonly ushort minLength = minLength;
    public readonly ushort maxLength = maxLength;
    public readonly ushort radius = radius;
    public readonly ushort lengthDecrease = lengthDecrease;
    public readonly float minLengthRatio = minLengthRatio;
    public readonly STALACTITE_GROWTH_DIR growthDir = growthDir;
    public readonly VoxelType voxelType = voxelType;
}

/**
 * Generated stalactite model.
 */
public struct Stalactite(VoxelItem[] voxels, STALACTITE_GROWTH_DIR growthDir, int id)
{
    public readonly int id = id;
    public readonly VoxelItem[] voxels = voxels;
    public readonly STALACTITE_GROWTH_DIR growthDir = growthDir;
}

/**
 * Class allowing generation of stalactites and stalagmites
 */
public static class StalactiteGenerator
{
    /**
     * Generates stalactite (stalagmite inverted) voxel model based on supplied definition.
     * Stalactite is generated from a circle in XZ plane. Each spot in the circle shoots out a spike of
     * voxels in Y dimension, their length decreases with distance from the center.
     */
    public static VoxelItem[] Generate(StalactiteDefinition stalactiteDef)
    {
        List<VoxelItem> stalactiteVoxels = [];
        Random random = new Random(stalactiteDef.seed + stalactiteDef.id);

        // obtain maximum length of the center spike
        short maxLength = (short)random.Next(stalactiteDef.minLength, stalactiteDef.maxLength + 1);

        // select growth direction
        short growthDirection = (short)(stalactiteDef.growthDir == STALACTITE_GROWTH_DIR.UP ? 1 : -1);
        // select spike starting y offset, stalactites pointing down must be moved by one voxel
        short spikeStartYOffset = (short)(stalactiteDef.growthDir == STALACTITE_GROWTH_DIR.UP ? 0 : 1);

        ushort radius = stalactiteDef.radius;

        // generate a circular area of stalactite voxel spikes around the center (0, 0, 0) in the XZ plane
        for (short x = (short)-radius; x <= radius; x++)
        {
            for (short z = (short)-radius; z <= radius; z++)
            {
                double distanceToCenter = Math.Sqrt(x * x + z * z);
                if (distanceToCenter > radius)
                    continue;

                // upper limit of spike length decreases with distance from the center
                double normalizedDistance = distanceToCenter / radius;
                short positionMaxLength = (short)Math.Round(maxLength * (1.0 - normalizedDistance * stalactiteDef.lengthDecrease / radius));
                if (positionMaxLength <= 0)
                    continue;

                // lower limit of spike length 
                short positionMinLength = (short)Math.Round(positionMaxLength * stalactiteDef.minLengthRatio);

                short columnLength = (short)random.Next(positionMinLength, positionMaxLength + 1);

                // generate voxel spike for this position
                for (short i = spikeStartYOffset; i < columnLength; i++)
                {
                    short y = (short)(growthDirection * i);
                    stalactiteVoxels.Add(new VoxelItem(x, y, z, stalactiteDef.voxelType));
                }
            }
        }

        return stalactiteVoxels.ToArray();
    }
}
