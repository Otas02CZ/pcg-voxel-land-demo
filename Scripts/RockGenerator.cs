// FILE: RockGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains a simple RockGenerator class for generation of rock voxel models
 
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Structure holding parameters for RockGenerator
 */ 
public struct RockDefinition(ushort sizeX, ushort sizeY, ushort sizeZ, ushort radiusMax, int id = 0, int seed = 5000)
{
    public readonly ushort sizeX = sizeX;
    public readonly ushort sizeY = sizeY;
    public readonly ushort sizeZ = sizeZ;
    public readonly ushort radiusMax = radiusMax;
    public int seed = seed;
    public int id = id;
}

/**
 * Generated Rock model.
 */
public class Rock(VoxelItem[] voxels, short offsetY, int id)
{
    public readonly int id = id;
    public readonly VoxelItem[] voxels = voxels;
    public readonly short offsetY = offsetY;
}

/**
 * Simple generator of rocks, uses 3D noise and depth gradient
 */
public static class RockGenerator
{
    /**
     * Initialize noise with given seed
     */
    private static FastNoiseLite InitializeNoise(int seed)
    {
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetSeed(seed);
        noise.SetFrequency(0.033f);
        noise.SetFractalLacunarity(1.55f);
        noise.SetFractalGain(0.7f);
        noise.SetFractalOctaves(4);
        noise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        noise.SetFractalType(FastNoiseLite.FractalTypeEnum.Fbm);

        return noise;
    }
    
    /**
     * Generates a rock model with given parameters.
     * Rock is generated as 3D noise limited in spreading from the center by outwards increasing spherical gradient.
     */
    public static (VoxelItem[], short offsetY) Generate(int seed, ushort sizeX, ushort sizeY, ushort sizeZ, ushort radiusMax)
    {
        FastNoiseLite noise = InitializeNoise(seed);
        
        VoxelModel rockModel = new VoxelModel(sizeX, sizeY, sizeZ);
        short centerX = (short)(sizeX / 2);
        short centerY = (short)(sizeY / 2);
        short centerZ = (short)(sizeZ / 2);
        short radiusMaxInt = (short)radiusMax;

        for (short x = (short)-centerX; x < centerX; x++)
        {
            for (short y = (short)-centerY; y < centerY; y++)
            {
                for (short z = (short)-centerZ; z < centerZ; z++)
                {
                    // get gradient
                    float gradient = GetGradient(x,y,z, radiusMaxInt);
                    
                    if (gradient >= 1) // definitely will not place anything
                        continue;
                    
                    // get noise value
                    float noiseValue = GetNoiseValue(noise, x, y, z);
                    
                    // add voxel, when noise is stronger
                    // chance reduces with distance from the center of the rock model
                    if (noiseValue > gradient)
                    {
                        rockModel.SetVoxel(x, (short)(y + centerY), z, VoxelType.STONE);
                    }
                }
            }
        }
        
        short offsetY = (short)-centerY;

        return (rockModel.GetVoxelItems(), offsetY);
    }

    /**
     * Returns noise at x, y, z position in range 0 - 1
     */
    private static float GetNoiseValue(FastNoiseLite noise, short x, short y, short z)
    {
        float noiseValue = noise.GetNoise3D(x, y, z);
        noiseValue = noiseValue * 0.5f + 0.5f; // scale to 0 - 1
        return noiseValue;
    }
    
    /**
     * Returns value of outwards increasing spherical gradient at x, y, z position
     */
    private static float GetGradient(short x, short y, short z, float radius)
    {
        float distance = Mathf.Sqrt(x * x + y * y + z * z);
        float gradient = distance / radius;
        return gradient;
    }
}
