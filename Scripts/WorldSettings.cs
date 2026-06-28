// FILE: WorldService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains structures for storing and handling world configuration and its presets.

using System;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Preset of WorldSettings configuration. On top of WorldSettings itself contains
 * overrides for some parameters that would require conversion from world generator
 * to ui display format.
 */
public struct WorldSettingsPreset(string name, WorldSettings worldSettings, int[] ageDist, int[] heightDist, int [] waterDist, float cavesSpaceRatio, float cavesTunnelsRatio, float cavesTunnelEntRatio, float snowThreshold)
{
    public string name { get; set; } = name;
    public WorldSettings settings { get; set; } = worldSettings;
    // overrides for distribution and ratio inputs, these would have to be converted in both ways
    // distribution are proportions of areas ex. values (1,1,1,1,1,1,1,1) will give equal distrib. to all age areas
    // (2,1,1,1,1,1,1,1) will double the proportion of very young areas compared to the rest, ...
    // ratio inputs are to be inverted to noise thresholds (higher values actually mean lower chance)
    public int[] ageDistributionInputs { get; set; } = ageDist;
    public int[] heightDistributionInputs { get; set; } = heightDist;
    public int[] waterAvailabilityDistributionInputs { get; set; } = waterDist;
    public float cavesSpaceRatioInput {get; set;} = cavesSpaceRatio;
    public float cavesTunnelsChanceRatioInput {get; set;} = cavesTunnelsRatio;
    public float cavesTunnelEntrancesChanceRatioInput {get; set;} = cavesTunnelEntRatio;
    public float snowThresholdRatioInput { get; set; } = snowThreshold;
}

/**
 * Structure holding water generator parameters related to lake related.
 * Includes conversion method from enumeration types to sets of predefined values.
 */
public struct LakeSize(int maxLakeDepth = 20, int maxLakeSize = 4000)
{
    public int maxLakeDepth { get; set; } = maxLakeDepth;
    public int maxLakeSize { get; set; } = maxLakeSize;
    
    /**
     * Returns LakeSize struct with preconfigured parameters of selected lake config type.
     */
    public static LakeSize FromLakeSizeType(LakeSizeType lakeSizeType)
    {
        return lakeSizeType switch
        {
            LakeSizeType.NONE => new LakeSize(0, 0),
            LakeSizeType.SMALL => new LakeSize(8, 250),
            LakeSizeType.MEDIUM => new LakeSize(12, 900),
            LakeSizeType.LARGE => new LakeSize(20, 2600),
            _ => new LakeSize(12, 900)
        };
    }
    
    /**
     * Returns text representation of lake size type for ui.
     */
    public static string ToString(LakeSizeType lakeSizeType)
    {
        return lakeSizeType switch
        {
            LakeSizeType.NONE => "None",
            LakeSizeType.SMALL => "Small",
            LakeSizeType.MEDIUM => "Medium",
            LakeSizeType.LARGE => "Large",
            _ => "Large"
        };
    }
}

/**
 * Set of preconfigured lake sizes.
 */
public enum LakeSizeType : byte
{
    NONE,
    SMALL,
    MEDIUM,
    LARGE
}

/**
 * Structure holding water generator parameters related to water availability.
 * Includes conversion method from enumeration types.
 */
public struct WaterAvailEffectStrength(int maxWaterInfluenceDistance = 48, byte maxWaterInfluence = 128, byte baseInfluence = 80)
{
    public int maxWaterInfluenceDistance { get; set; } = maxWaterInfluenceDistance;
    public byte maxWaterInfluence { get; set; } = maxWaterInfluence;
    public byte baseInfluence { get; set; } = baseInfluence;

    /**
     * Returns WaterAvailabilityEffectStrength struct with predefined parameters for given effect type.
     */
    public static WaterAvailEffectStrength FromWaterAvailEffectStrengthType(WaterAvailEffectStrengthType effectType)
    {
        return effectType switch
        {
            WaterAvailEffectStrengthType.LOW => new WaterAvailEffectStrength(36, 128, 60),
            WaterAvailEffectStrengthType.MEDIUM => new WaterAvailEffectStrength(48, 128, 80),
            WaterAvailEffectStrengthType.HIGH => new WaterAvailEffectStrength(64, 148, 110),
            _ => new WaterAvailEffectStrength(48, 128, 80)
        };
    }
    
    /**
     * Returns text representation of lake size type for ui.
     */
    public static string ToString(WaterAvailEffectStrengthType effectType)
    {
        return effectType switch
        {
            WaterAvailEffectStrengthType.LOW => "Low",
            WaterAvailEffectStrengthType.MEDIUM => "Medium",
            WaterAvailEffectStrengthType.HIGH => "High",
            _ => "High"
        };
    }
}

/**
 * Set of preconfigured water availability effect strengths
 */
public enum WaterAvailEffectStrengthType : byte
{
    LOW,
    MEDIUM,
    HIGH
}

/**
 * Structure holding terrain height noise configuration parameters.
 * Includes conversion method from enumeration types.
 */
public struct HeightNoiseConfig(float heightNoiseFrequency = 0.0024f, float heightNoiseLacunarity = 1.82f, float heightNoiseFractalGain = 0.56f, int heightNoiseOctaves = 10)
{
    public float heightNoiseFrequency { get; set; } = heightNoiseFrequency;
    public float heightNoiseLacunarity { get; set; } = heightNoiseLacunarity;
    public float heightNoiseFractalGain { get; set; } = heightNoiseFractalGain;
    public int heightNoiseOctaves { get; set; } = heightNoiseOctaves;
    
    /**
     * Returns HeightNoiseConfig struct with predefined parameters for given effect type.
     */
    public static HeightNoiseConfig FromHeightNoiseConfigType(HeightNoiseConfigType configType)
    {
        return configType switch
        {
            HeightNoiseConfigType.SMOOTH => new HeightNoiseConfig(0.0020f, 1.78f, 0.54f, 8),
            HeightNoiseConfigType.NORMAL => new HeightNoiseConfig(0.0024f, 1.82f, 0.56f, 10),
            HeightNoiseConfigType.ROUGH => new HeightNoiseConfig(0.003f, 1.85f, 0.57f, 11),
            _ => new HeightNoiseConfig(0.0024f, 1.82f, 0.56f, 10)
        };
    }
    
    /**
     * Returns text representation of lake size type for ui.
     */
    public static string ToString(HeightNoiseConfigType configType)
    {
        return configType switch
        {
            HeightNoiseConfigType.NORMAL => "Normal",
            HeightNoiseConfigType.SMOOTH => "Smooth",
            HeightNoiseConfigType.ROUGH => "Rough",
            _ => "Smooth"
        };
    }
}

/**
 * Set of preconfigured height noise configs.
 */
public enum HeightNoiseConfigType : byte
{
    NORMAL,
    SMOOTH,
    ROUGH
}

/**
 * Structure holding rng ranges for placement chance of vegetation and decoration in areas of all water availability types.
 * Includes conversion method from enumeration types.
 */
public struct VegetationChance(int high = 12000, int medium = 14250, int low = 16500, int arid = 25000)
{
    public int vegetationRandomMaxHighWater { get; set; } = high;
    public int vegetationRandomMaxMediumWater { get; set; } = medium;
    public int vegetationRandomMaxLowWater { get; set; } = low;
    public int vegetationRandomMaxArid { get; set; } = arid;
    
    /**
     * Returns VegetationChance struct with predefined parameters for given effect type.
     */
    public static VegetationChance FromOverallVegetationChanceType(VegChanceType chanceType)
    {
        return chanceType switch
        {
            VegChanceType.VERY_LOW => new VegetationChance(20000, 23000, 26000, 40000),
            VegChanceType.LOW => new VegetationChance(16000, 19000, 22000, 35000),
            VegChanceType.MEDIUM => new VegetationChance(14000, 16500, 20000, 30000),
            VegChanceType.HIGH => new VegetationChance(12000, 14250, 16500, 25000),
            VegChanceType.VERY_HIGH => new VegetationChance(10000, 11500, 13000, 21000),
            _ => new VegetationChance(12000, 14250, 16500, 25000)
        };
    }
    
    /**
     * Returns text representation of lake size type for ui.
     */
    public static string ToString(VegChanceType chanceType)
    {
        return chanceType switch
        {
            VegChanceType.VERY_LOW => "Very Low",
            VegChanceType.LOW => "Low",
            VegChanceType.MEDIUM => "Medium",
            VegChanceType.HIGH => "High",
            VegChanceType.VERY_HIGH => "Very High",
            _ => "Medium"
        };
    }
}

/**
 * Set of preconfigured vegetation chance configurations.
 */
public enum VegChanceType : byte
{
    VERY_LOW,
    LOW,
    MEDIUM,
    HIGH,
    VERY_HIGH
}

/**
 * Encapsulates all user configurable parameters of all generators in the demo.
 */
public class WorldSettings
{
    public int seed { get; set; } = 5000;
    public byte terrainChunkCountY { get; set; } = 24;
    // caves
    public bool cavesEnabled { get; set; } = true;
    public float cavesStartTerrainHeightRatio { get; set; } = 0.35f;
    public float cavesWaterLevelTerrainHeightRatio { get; set; } = 0.02f;
    public float cavesSpaceAmount { get; set; } = 0.625f;
    public float cavesTunnelsChance { get; set; } = 0.725f;
    public float cavesTunnelEntrancesChance { get; set; } = 0.65f;
    public int cavesStalactitesChance { get; set; } = 110; // of 1000
    public int cavesWaterfallChance { get; set; } = 5; // of 1000
    // water
    public float explicitWaterLevelTerrainHeightRatio { get; set; } = 0.025f;
    public LakeSizeType lakeSize { get; set; } = LakeSizeType.LARGE;
    public int maxRiverLength { get; set; } = 500;
    public WaterAvailEffectStrengthType waterAvailabilityStrength { get; set; } = WaterAvailEffectStrengthType.HIGH;
    public byte explicitWaterAvailabilityMaxEffect { get; set; } = 160;
    // world
    public HeightNoiseConfigType heightNoiseConfig { get; set; } = HeightNoiseConfigType.NORMAL;
    public float[] areaAgeThresholds { get; set; } = [-0.7f, -0.4f, -0.15f, 0.0f, 0.225f, 0.5f, 0.75f];
    public float[] areaHeightThresholds { get; set; } = [0.3f, 0.55f];
    public byte[] areaWaterAvailabilityThresholds { get; set; } = [60, 90, 140];
    public int waterLilyChance { get; set; } = 22; // of 10000
    public int waterReedsChance { get; set; } = 230; // of 10000
    public VegChanceType overallVegetationChance { get; set; } = VegChanceType.HIGH;
    public float snowChance { get; set; } = 0.75f;

    // limits
    public const int seedMin = int.MinValue;
    public const int seedMax = int.MaxValue;
    public const byte terrainChunkCountYMin = 12;
    public const byte terrainChunkCountYMax = 32;
    public const float cavesStartTerrainHeightRatioMin = 0.05f;
    public const float cavesStartTerrainHeightRatioMax = 0.5f;
    public const float cavesWaterLevelTerrainHeightRatioMin = 0.01f;
    public const float cavesWaterLevelTerrainHeightRatioMax = 0.1f;
    public const float cavesSpaceAmountMin = 0.45f;
    public const float cavesSpaceAmountMax = 0.65f;
    public const float cavesTunnelsChanceMin = 0.525f;
    public const float cavesTunnelsChanceMax = 0.75f;
    public const float cavesTunnelEntrancesChanceMin = 0.6f;
    public const float cavesTunnelEntrancesChanceMax = 0.75f;
    public const int cavesStalactitesChanceMin = 0;
    public const int cavesStalactitesChanceMax = 250;
    public const int cavesWaterfallChanceMin = 0;
    public const int cavesWaterfallChanceMax = 25;
    public const float explicitWaterLevelTerrainHeightRatioMin = 0.01f;
    public const float explicitWaterLevelTerrainHeightRatioMax = 0.5f;
    public const int maxRiverLengthMin = 5;
    public const int maxRiverLengthMax = 1000;
    public const byte explicitWaterAvailabilityMaxEffectMin = byte.MinValue;
    public const byte explicitWaterAvailabilityMaxEffectMax = byte.MaxValue;
    public const int areaAgeThresholdsCount = 7;
    public const float areaAgeThresholdsMin = 0.0f;
    public const float areaAgeThresholdsMax = 1.0f;
    public const int areaHeightThresholdsCount = 2;
    public const float areaHeightThresholdsMin = 0;
    public const float areaHeightThresholdsMax = 1;
    public const int areaWaterAvailabilityThresholdsCount = 3;
    public const byte areaWaterAvailabilityThresholdsMin = 0;
    public const byte areaWaterAvailabilityThresholdsMax = 255;
    public const int waterLilyChanceMin = 0;
    public const int waterLilyChanceMax = 100;
    public const int waterReedsChanceMin = 0;
    public const int waterReedsChanceMax = 500;
    public const float snowChanceMin = 0f;
    public const float snowChanceMax = 1f;
    // used for parameter distributions - age, height, water availability
    public const int distributionInputsMin = 0;
    public const int distributionInputsMax = 100;
    // used for ratios that are later on converted to noise thresholds
    public const float ratioInputsMin = 0f;
    public const float ratioInputsMax = 1f;

    // VALIDATION FUNCTIONS FOR PARAMETERS
    
    private void ClampTerrainChunkCountY()
    {
        terrainChunkCountY = Math.Clamp(terrainChunkCountY, terrainChunkCountYMin, terrainChunkCountYMax);
    }
    
    private void ClampCavesStartTerrainHeightRatio()
    {
        cavesStartTerrainHeightRatio = Math.Clamp(cavesStartTerrainHeightRatio, cavesStartTerrainHeightRatioMin, cavesStartTerrainHeightRatioMax);
    }
    
    private void ClampCavesWaterLevelTerrainHeightRatio()
    {
        cavesWaterLevelTerrainHeightRatio = Math.Clamp(cavesWaterLevelTerrainHeightRatio, cavesWaterLevelTerrainHeightRatioMin, cavesWaterLevelTerrainHeightRatioMax);
    }
    
    private void ClampCavesSpaceAmount()
    {
        cavesSpaceAmount = Math.Clamp(cavesSpaceAmount, cavesSpaceAmountMin, cavesSpaceAmountMax);
    }
    
    private void ClampCavesTunnelsChance()
    {
        cavesTunnelsChance = Math.Clamp(cavesTunnelsChance, cavesTunnelsChanceMin, cavesTunnelsChanceMax);
    }
    
    private void ClampCavesTunnelEntrancesChance()
    {
        cavesTunnelEntrancesChance = Math.Clamp(cavesTunnelEntrancesChance, cavesTunnelEntrancesChanceMin, cavesTunnelEntrancesChanceMax);
    }
    
    private void ClampCavesStalactitesCeilingChance()
    {
        cavesStalactitesChance = Math.Clamp(cavesStalactitesChance, cavesStalactitesChanceMin, cavesStalactitesChanceMax);
    }
    
    private void ClampCavesWaterfallChance()
    {
        cavesWaterfallChance = Math.Clamp(cavesWaterfallChance, cavesWaterfallChanceMin, cavesWaterfallChanceMax);
    }
    
    private void ClampExplicitWaterLevelTerrainHeightRatio()
    {
        explicitWaterLevelTerrainHeightRatio = Math.Clamp(explicitWaterLevelTerrainHeightRatio, explicitWaterLevelTerrainHeightRatioMin, explicitWaterLevelTerrainHeightRatioMax);
    }
    
    private void ClampMaxRiverLength()
    {
        maxRiverLength = Math.Clamp(maxRiverLength, maxRiverLengthMin, maxRiverLengthMax);
    }
    
    private void ClampUndergroundWaterMaxEffect()
    {
        explicitWaterAvailabilityMaxEffect = Math.Clamp(explicitWaterAvailabilityMaxEffect, explicitWaterAvailabilityMaxEffectMin, explicitWaterAvailabilityMaxEffectMax);
    }
    
    public bool ValidateThresholdsArrays()
    {
        return areaAgeThresholds.Length == areaAgeThresholdsCount &&
               areaHeightThresholds.Length == areaHeightThresholdsCount &&
               areaWaterAvailabilityThresholds.Length == areaWaterAvailabilityThresholdsCount;
    }
    
    private void ClampAreaAgeThresholds()
    {
        for (int i = 0; i < areaAgeThresholds.Length; i++)
        {
            areaAgeThresholds[i] = Math.Clamp(areaAgeThresholds[i], areaAgeThresholdsMin, areaAgeThresholdsMax);
        }
    }
    
    private void ClampAreasHeightThresholds()
    {
        for (int i = 0; i < areaHeightThresholds.Length; i++)
        {
            areaHeightThresholds[i] = Math.Clamp(areaHeightThresholds[i], areaHeightThresholdsMin, areaHeightThresholdsMax);
        }
    }
    
    private void ClampAreasWaterAvailabilityThresholds()
    {
        for (int i = 0; i < areaWaterAvailabilityThresholds.Length; i++)
        {
            areaWaterAvailabilityThresholds[i] = Math.Clamp(areaWaterAvailabilityThresholds[i], areaWaterAvailabilityThresholdsMin, areaWaterAvailabilityThresholdsMax);
        }
    }
    
    private void ClampWaterLilyChance()
    {
        waterLilyChance = Math.Clamp(waterLilyChance, waterLilyChanceMin, waterLilyChanceMax);
    }
    
    private void ClampWaterReedsChance()
    {
        waterReedsChance = Math.Clamp(waterReedsChance, waterReedsChanceMin, waterReedsChanceMax);
    }
    
    private void ClampSnowChance()
    {
        snowChance = Math.Clamp(snowChance, snowChanceMin, snowChanceMax);
    }
    
    /**
     * Clamps all parameters to predefined limits.
     */
    public void ClampAll()
    {
        ClampTerrainChunkCountY();
        ClampCavesStartTerrainHeightRatio();
        ClampCavesWaterLevelTerrainHeightRatio();
        ClampCavesSpaceAmount();
        ClampCavesTunnelsChance();
        ClampCavesTunnelEntrancesChance();
        ClampCavesStalactitesCeilingChance();
        ClampCavesWaterfallChance();
        ClampExplicitWaterLevelTerrainHeightRatio();
        ClampMaxRiverLength();
        ClampUndergroundWaterMaxEffect();
        ClampAreaAgeThresholds();
        ClampAreasHeightThresholds();
        ClampAreasWaterAvailabilityThresholds();
        ClampWaterLilyChance();
        ClampWaterReedsChance();
        ClampSnowChance();
    }
}

/**
 * Encapsulates world settings with other data needed for saving information about single world.
 */
public class WorldSaveConfig
{
    public string worldName { get; set; } = "default";
    public string worldDirectoryName { get; set; } = "default";
    public WorldSettings worldSettings { get; set; } = new();
    public Vector3Double lastPlayerPosition { get; set; } = new(0, 0, 0);
}
