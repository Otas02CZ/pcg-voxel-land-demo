// FILE: VegetationGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains VegetationGenerator class that allows generation of various types of plant voxel models.

using System;
using System.Collections.Generic;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Structure with grass generator parameters
 */
public struct GrassDefinition(
    ushort minSteps,
    ushort maxSteps,
    ushort minBlades,
    ushort maxBlades,
    ushort areaX,
    ushort areaZ,
    VoxelType voxelType,
    int seed = 5000,
    float dxMin = -0.4f,
    float dxMax = 0.4f,
    float dyMin = 0.6f,
    float dyMax = 1.0f,
    int id = 0)
{
    public int seed = seed;
    public int id = id;
    public readonly VoxelType voxelType = voxelType;
    public readonly ushort minSteps = minSteps;
    public readonly ushort maxSteps = maxSteps;
    public readonly ushort minBlades = minBlades;
    public readonly ushort maxBlades = maxBlades;
    public readonly ushort areaX = areaX;
    public readonly ushort areaZ = areaZ;
    public readonly float dxMin = dxMin;
    public readonly float dxMax = dxMax;
    public readonly float dyMin = dyMin;
    public readonly float dyMax = dyMax;
}

/**
 * Subtypes of plants.
 * All of these share the same definition structure.
 */
public enum PLANT_TYPE : byte
{
    POPPY,
    BLUEBELL,
    DAISY,
    DANDELION,
    FERN,
    BURDOCK
}

/**
 * Parameters for generation of plants, shared by all PLANT_TYPEs
 */
public struct PlantDefinition(
    PLANT_TYPE plantType,
    ushort minLength,
    ushort maxLength,
    ushort minStemLength = 4,
    ushort maxStemLength = 9,
    ushort leafChance = 90, // out of 100
    float stemDxMin = -0.2f,
    float stemDxMax = 0.2f,
    VoxelType voxelType = VoxelType.PLANT,
    int seed = 5000,
    int id = 0)
{
    public int seed = seed;
    public int id = id;
    public readonly ushort minLength = minLength;
    public readonly ushort maxLength = maxLength;
    public readonly ushort minStemLength = minStemLength;
    public readonly ushort maxStemLength = maxStemLength;
    public readonly ushort leafChance = leafChance;
    public readonly float stemDxMin = stemDxMin;
    public readonly float stemDxMax = stemDxMax;
    public readonly VoxelType voxelType = voxelType;
    public readonly PLANT_TYPE plantType = plantType;
}

/**
 * Parameters for generation of flower plant bushes
 */
public struct PlantBushDefinition(
    VoxelType trunkVoxelType = VoxelType.WOOD,
    VoxelType leafVoxelType = VoxelType.PLANT,
    VoxelType flowerVoxelType = VoxelType.RED,
    float stemDxMin = -0.75f,
    float stemDxMax = 0.75f,
    ushort maxHeight = 32,
    int splitIntoCountMin = 2,
    int splitIntoCountMax = 4,
    int splitChance = 300,
    int initialBranchEndChance = 0,
    int initialLeafChance = 0,
    int initialFlowerChance = 0,
    int leafChanceChange = 35,
    int flowerChanceChange = 4,
    int branchEndChanceChange = 30,
    int seed = 5000,
    int id = 0)
{
    public int seed = seed;
    public int id = id;
    public readonly VoxelType trunkVoxelType = trunkVoxelType;
    public readonly VoxelType leafVoxelType = leafVoxelType;
    public readonly VoxelType flowerVoxelType = flowerVoxelType;
    public readonly float stemDxMin = stemDxMin;
    public readonly float stemDxMax = stemDxMax;
    public readonly ushort maxHeight = maxHeight;
    public readonly int splitIntoCountMin = splitIntoCountMin; // minimum number of branches to split into
    public readonly int splitIntoCountMax = splitIntoCountMax; // maximum number of branches to split into
    // chances out of 1000
    public readonly int splitChance = splitChance; // chance for a branch to split at each step
    public readonly int initialBranchEndChance = initialBranchEndChance; // chance for a branch to end at each step
    public readonly int initialLeafChance = initialLeafChance; //  chance to place a leaf at current yLevel for each of branch 4 voxel neighbors
    public readonly int initialFlowerChance = initialFlowerChance; // chance to place a flower at current yLevel for each of branch 4 voxel neighbors
    public readonly int leafChanceChange = leafChanceChange; // how much the leaf chance changes per yLevel
    public readonly int flowerChanceChange = flowerChanceChange; // how much the flower chance changes per yLevel
    public readonly int branchEndChanceChange = branchEndChanceChange; // how much the branch end chance changes per yLevel
}

/**
 * Parameters for generation of reed-like water plants.
 */
public struct ReedDefinition(
    ushort minSteps,
    ushort maxSteps,
    ushort minBlades,
    ushort maxBlades,
    ushort areaX,
    ushort areaZ,
    VoxelType voxelTypeStem,
    VoxelType voxelTypeSeeds, // and flowers
    ushort seedsChance = 50,
    float dxMin = -0.25f,
    float dxMax = 0.25f,
    float dyMin = 0.6f,
    float dyMax = 1.0f,
    int seed = 5000,
    int id = 0)
{
    public int seed = seed;
    public int id = id;
    public readonly VoxelType voxelTypeStem = voxelTypeStem;
    public readonly VoxelType voxelTypeSeeds = voxelTypeSeeds;
    public readonly ushort minSteps = minSteps;
    public readonly ushort maxSteps = maxSteps;
    public readonly ushort minBlades = minBlades;
    public readonly ushort maxBlades = maxBlades;
    public readonly ushort areaX = areaX;
    public readonly ushort areaZ = areaZ;
    public readonly ushort seedsChance = seedsChance;
    public readonly float dxMin = dxMin;
    public readonly float dxMax = dxMax;
    public readonly float dyMin = dyMin;
    public readonly float dyMax = dyMax;
}

/**
 * Parameters of generation water lilies.
 */
public struct WaterLilyDefinition(
    VoxelType voxelTypeLeaves,
    VoxelType voxelTypeLeavesAlt,
    VoxelType voxelTypeFlowerInner,
    VoxelType voxelTypeFlowerOuter,
    ushort minLeaves,
    ushort maxLeaves,
    ushort minLeafRadius,
    ushort maxLeafRadius,
    int seed = 5000,
    int id = 0)
{
    public int seed = seed;
    public int id = id;
    public readonly VoxelType voxelTypeLeaves = voxelTypeLeaves;
    public readonly VoxelType voxelTypeLeavesAlt = voxelTypeLeavesAlt;
    public readonly VoxelType voxelTypeFlowerInner = voxelTypeFlowerInner;
    public readonly VoxelType voxelTypeFlowerOuter = voxelTypeFlowerOuter;
    public readonly ushort minLeaves = minLeaves;
    public readonly ushort maxLeaves = maxLeaves;
    public readonly ushort minLeafRadius = minLeafRadius;
    public readonly ushort maxLeafRadius = maxLeafRadius;
}

/**
 * Class encapsulating generation process of many plant voxel models.
 */
public static class VegetationGenerator
{
    /**
     * Generates grass voxel model based on supplied definition.
     * Grass cluster is generated with a set of blades. Each blade has randomized starting position
     * in the XZ plane and is generated as a line with randomized increments in each axis of growth.
     */
    public static VoxelItem[] GenerateGrass(GrassDefinition grassDef)
    {
        Random random = new Random(grassDef.seed + grassDef.id);
        List<VoxelItem> plant = [];
        // obtain random count of grass blades to place
        int blades = random.Next(grassDef.minBlades, grassDef.maxBlades);
        
        for (int blade = 0; blade < blades; blade++)
        {
            // choose blade start position from rectangular area around center
            Vector3Short bladeStart = new Vector3Short((short)random.Next(-grassDef.areaX/2, grassDef.areaX/2), 0, (short)random.Next(-grassDef.areaZ/2, grassDef.areaZ/2));
            // choose blade generation steps
            short generationSteps = (short)random.Next(grassDef.minSteps, grassDef.maxSteps);
            Vector3Short currentPos = bladeStart;
            plant.Add(new VoxelItem(bladeStart.x, bladeStart.y, bladeStart.z, grassDef.voxelType));

            // randomized deltas for increment of each axis per generation step
            float dx = random.NextSingle() * (grassDef.dxMax - grassDef.dxMin) + grassDef.dxMin;
            float dz = random.NextSingle() * (grassDef.dxMax - grassDef.dxMin) + grassDef.dxMin;
            float dy = random.NextSingle() * (grassDef.dyMax - grassDef.dyMin) + grassDef.dyMin;
            float actualX = currentPos.x;
            float actualZ = currentPos.y;
            float actualY = currentPos.z;
            
            // generate blade, step by step
            for (int l = 1; l < generationSteps; l++)
            {
                actualX += dx;
                actualZ += dz;
                actualY += dy;
                currentPos.x = (short)Math.Round(actualX);
                currentPos.z = (short)Math.Round(actualZ);
                currentPos.y = (short)Math.Round(actualY);
                plant.Add(new VoxelItem(currentPos.x, currentPos.y, currentPos.z, VoxelType.PLANT));
            }
        }

        return plant.ToArray();
    }

    /**
     * Generates simple flower with leaves and stem without flower head.
     * Returns start position of the flower head.
     */
    private static Vector3Short GenerateSimpleFlowerStemLeaves(List<VoxelItem> plant, PlantDefinition plantDef)
    {
        Random random = new Random(plantDef.seed + plantDef.id);
        
        // randomized stem height
        ushort stemLength = (ushort)random.Next(plantDef.minStemLength, plantDef.maxStemLength);
        // obtain stem growth increments in X and Z axis
        float stemDx = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
        float stemDz = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
        Vector3Short currentPos = new Vector3Short(0, 0, 0);
        
        // generate stem
        float stemX = 0;
        float stemZ = 0;
        for (ushort i = 0; i < stemLength; i++)
        {
            stemX += stemDx;
            stemZ += stemDz;
            currentPos.x = (short)Math.Round(stemX);
            currentPos.z = (short)Math.Round(stemZ);
            currentPos.y = (short)i;
            plant.Add(new VoxelItem(currentPos.x, currentPos.y, currentPos.z, VoxelType.PLANT));
            
        }
        
        // generate leaves around the bottom
        GenerateSimpleLeaves(plant, random, plantDef.minLength, plantDef.maxLength, plantDef.leafChance);

        currentPos.y++;
        return currentPos;
    }
    
    /**
     * Adds randomized set of leaves growing from the bottom of the plant model.
     */
    private static void GenerateSimpleLeaves(List<VoxelItem> plant, Random random, ushort minLength, ushort maxLength, ushort leafChance = 80)
    {
        // possible leaf growth directions
        List<Vector2Short> directions =
        [
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        ];
        
        // for each leaf
        foreach (Vector2Short direction in directions)
        {
            // check whether this one will be generated
            if (random.Next(100) > leafChance)
                continue; // skip this leaf
            
            // choose leaf length
            ushort leafLength = (ushort)random.Next(minLength, maxLength);
            
            Vector3Short currentPos = new Vector3Short(0, -1, 0);
            // generate the leaf
            for (int l = 1; l <= leafLength; l++)
            {
                currentPos.x += direction.x;
                currentPos.z += direction.y;
                currentPos.y += 1; // always going up
                plant.Add(new VoxelItem(currentPos.x, currentPos.y, currentPos.z, VoxelType.PLANT));
            }
        }
    }

    /**
     * Generates a poppy flower consisting of central stem that ends in predefined flower head.
     * And a set of leaves with randomized length.
     */
    public static VoxelItem[] GeneratePoppy(PlantDefinition plantDef)
    {
        List<VoxelItem> plant = [];
        // generate stem and leaves of the flower
        Vector3Short lastPosition = GenerateSimpleFlowerStemLeaves(plant, plantDef);
        
        // flower head
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.RED));
        plant.Add(new VoxelItem((short)(lastPosition.x - 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.RED));
        plant.Add(new VoxelItem((short)(lastPosition.x + 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.RED));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z - 1), VoxelType.RED));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 1), VoxelType.RED));
        
        return plant.ToArray();
    }
    
    /**
     * Generates a dandelion flower consisting of central stem that ends in predefined flower head.
     * And a set of leaves with randomized length.
     */
    public static VoxelItem[] GenerateDandelion(PlantDefinition plantDef)
    {
        List<VoxelItem> plant = [];
        // generate stem and leaves of the flower
        Vector3Short lastPosition = GenerateSimpleFlowerStemLeaves(plant, plantDef);
        
        // flower head
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.YELLOW));
        plant.Add(new VoxelItem((short)(lastPosition.x - 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.YELLOW));
        plant.Add(new VoxelItem((short)(lastPosition.x + 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.YELLOW));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z - 1), VoxelType.YELLOW));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 1), VoxelType.YELLOW));
        
        return plant.ToArray();
    }

    /**
     * Generates a daisy flower consisting of central stem that ends in predefined flower head.
     * And a set of leaves with randomized length.
     */
    public static VoxelItem[] GenerateDaisy(PlantDefinition plantDef)
    {
        List<VoxelItem> plant = [];
        // generate stem and leaves of the flower
        Vector3Short lastPosition = GenerateSimpleFlowerStemLeaves(plant, plantDef);
        
        // flower head
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.YELLOW));
        plant.Add(new VoxelItem((short)(lastPosition.x - 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.WHITE));
        plant.Add(new VoxelItem((short)(lastPosition.x + 1), lastPosition.y, (short)(lastPosition.z + 0), VoxelType.WHITE));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z - 1), VoxelType.WHITE));
        plant.Add(new VoxelItem((short)(lastPosition.x + 0), lastPosition.y, (short)(lastPosition.z + 1), VoxelType.WHITE));
        
        return plant.ToArray();
    }
    
    /**
     * Generates bluebell flower.
     * Flower consists of central stem with flower voxels that alternate in X and Z axis and a set of leaves
     * growing from the bottom.
     */
    public static VoxelItem[] GenerateBluebell(PlantDefinition plantDef)
    {
        List<VoxelItem> plant = [];
        Random random = new Random(plantDef.seed + plantDef.id);
        
        // obtain stem length
        ushort stemLength = (ushort)random.Next(plantDef.minStemLength, plantDef.maxStemLength);
        bool flowerPosSwitch = false; // dictates current flower voxel axis X /Z
        ushort floweringStart = (ushort)(4 - random.Next(0, 2)); // how high flowers start appearing
        
        // generate stem with flowers
        ushort currentY = 0;
        for (; currentY < stemLength; currentY++)
        {
            plant.Add(new VoxelItem(0, (short)currentY, 0, VoxelType.PLANT)); // stem
            flowerPosSwitch = !flowerPosSwitch;
            // flowers
            if (currentY < floweringStart)
                continue;
            if (flowerPosSwitch) // flowers in +X -X direction
            {
                plant.Add(new VoxelItem(1, (short)currentY, 0, VoxelType.BLUE));
                plant.Add(new VoxelItem(-1, (short)currentY, 0, VoxelType.BLUE));
            }
            else // flowers in +Z -Z direction
            {
                plant.Add(new VoxelItem(0, (short)currentY, 1, VoxelType.BLUE));
                plant.Add(new VoxelItem(0, (short)currentY, -1, VoxelType.BLUE));
            }
        }

        // final flower on top
        plant.Add(new VoxelItem(0, (short)currentY, 0, VoxelType.BLUE));
        
        // add leaves growing from the bottom
        GenerateSimpleLeaves(plant, random, plantDef.minLength, plantDef.maxLength, plantDef.leafChance);
        
        return plant.ToArray();
    }

    /**
     * Generates fern/sapling like plant.
     * Central stem with blades at odd y positions in 8 XZ directions that shorten from bottom to top.
     */
    public static VoxelItem[] GenerateFern(PlantDefinition plantDef)
    {
        // set of directions for blade growth, smaller dictates that diagonal directions will be one voxel shorter
        List<(Vector2Short dir, bool smaller)> directions =
        [
            (new Vector2Short(1, 0), false),
            (new Vector2Short(-1, 0), false),
            (new Vector2Short(0, 1), false),
            (new Vector2Short(0, -1), false),
            (new Vector2Short(1, 1), true),
            (new Vector2Short(-1, 1), true),
            (new Vector2Short(1, -1), true),
            (new Vector2Short(-1, -1), true)
        ];
        
        List<VoxelItem> plant = [];
        Random random = new Random(plantDef.seed + plantDef.id);
        
        // generate stem length
        short stemLength = (short)random.Next(plantDef.minStemLength, plantDef.maxStemLength);
        ushort leafLength = 1;
        // generate fern from top to bottom
        for (short y = stemLength; y >= 0; y--)
        {
            if ((stemLength - y) % 2 == 1 && y != 0) // odd positions generate leaves (except the base)
            {
                // generate leaves
                foreach (var direction in directions)
                {
                    // generate leaf, diagonal (smaller) dictate one voxel shorted blades
                    for (ushort l = 1; l <= (direction.smaller ? leafLength-1 : leafLength); l++)
                    {
                        plant.Add(new VoxelItem((short)(direction.dir.x * l), y, (short)(direction.dir.y * l), plantDef.voxelType));
                    }
                }

                leafLength++;
            }
            // always generate stem voxel
            plant.Add(new VoxelItem(0, y, 0, plantDef.voxelType));
        }

        return plant.ToArray();
    }

    /**
     * Generates burdock like plant voxel model with oval leaves growing to alternating
     * sides of the stem.
     */
    public static VoxelItem[] GenerateBurdock(PlantDefinition plantDef)
    {
        List<Vector2Short> directions =
        [
            new (1, 0),
            new (-1, 0),
            new (0, 1),
            new (0, -1)
        ];
        
        List<VoxelItem> plant = [];
        Random random = new Random(plantDef.seed + plantDef.id);
        
        // stem length acts as leaf count
        ushort totalLeaves = (ushort)random.Next(plantDef.minStemLength, plantDef.maxStemLength);
        short currentY = 0;
        // generate leaves
        for (ushort i = 0; i < totalLeaves; i++)
        {
            // generate leaf in selected direction
            Vector2Short dir = directions[i % directions.Count];
            // obtain leaf length
            ushort leafLength = (ushort)random.Next(plantDef.minLength, plantDef.maxLength);
            
            // generate oval leaf
            for (ushort l = 0; l < leafLength; l++)
            {
                // obtain leaf width at this distance from leaf start
                ushort leafWidth = CalculateOvalWidth(l, leafLength);
                
                // calculate perpendicular direction for width expansion
                Vector2Short perpDir = new Vector2Short((short)-dir.y, dir.x);
                
                // leaf center line
                plant.Add(new VoxelItem((short)(dir.x * l), currentY, (short)(dir.y * l), plantDef.voxelType));
                
                // add width on both sides perpendicular to leaf direction
                for (int w = 1; w <= leafWidth; w++)
                {
                    plant.Add(new VoxelItem((short)(dir.x * l + perpDir.x * w), currentY, (short)(dir.y * l + perpDir.y * w), plantDef.voxelType));
                    plant.Add(new VoxelItem((short)(dir.x * l - perpDir.x * w), currentY, (short)(dir.y * l - perpDir.y * w), plantDef.voxelType));
                }
            }
            // stem center
            plant.Add(new VoxelItem(0, currentY, 0, plantDef.voxelType));
            currentY++;
        }
        // final stem voxel on top
        plant.Add(new VoxelItem(0, currentY, 0, plantDef.voxelType));
        
        return plant.ToArray();
    }
    
    /**
     * Calculate width of oval with specified total length at its given position
     */
    private static ushort CalculateOvalWidth(ushort position, ushort totalLength)
    {
        float leafLengthWidthRatio = 0.33f;
        if (totalLength <= 2)
            return 0;
        
        float midPoint = (totalLength - 1) / 2.0f;
        float distanceFromMid = Math.Abs(position - midPoint);
        float normalizedDist = distanceFromMid / midPoint;
        
        // calculate max width based on leaf length
        ushort maxWidth = (ushort)(Math.Max(1, totalLength * leafLengthWidthRatio));
        
        // width decreases with distance from midpoint
        float widthFactor = 1.0f - (normalizedDist * normalizedDist);
        ushort width = (ushort)Math.Round(maxWidth * widthFactor);
        
        return Math.Max((ushort)0, width);
    }

    /**
     * Generates reed-like water plants.
     * Very similar to grass generator, but potentially adds seeds / flowers at ends of blades
     */
    public static VoxelItem[] GenerateReed(ReedDefinition plantDef)
    {
        Random random = new Random(plantDef.seed);
        List<VoxelItem> plant = [];
        // obtain total amount of blades to place
        int blades = random.Next(plantDef.minBlades, plantDef.maxBlades);
        
        // generate each blade
        for (int blade = 0; blade < blades; blade++)
        {
            // choose blade start position
            Vector3Short bladeStart = new Vector3Short((short)random.Next(-plantDef.areaX/2, plantDef.areaX/2), 0, (short)random.Next(-plantDef.areaZ/2, plantDef.areaZ/2));
            // choose number of blade generation steps
            int generationSteps = random.Next(plantDef.minSteps, plantDef.maxSteps);
            Vector3Short currentPos = bladeStart;
            // starting voxel
            plant.Add(new VoxelItem(bladeStart.x, bladeStart.y, bladeStart.z, plantDef.voxelTypeStem));

            // randomized XYZ deltas for increment of each axis per generation step
            float dx = random.NextSingle() * (plantDef.dxMax - plantDef.dxMin) + plantDef.dxMin;
            float dz = random.NextSingle() * (plantDef.dxMax - plantDef.dxMin) + plantDef.dxMin;
            float dy = random.NextSingle() * (plantDef.dyMax - plantDef.dyMin) + plantDef.dyMin;
            float actualX = currentPos.x;
            float actualZ = currentPos.z;
            float actualY = currentPos.y;
            
            // generate a blade, step by step
            for (int l = 1; l < generationSteps; l++)
            {
                actualX += dx;
                actualZ += dz;
                actualY += dy;
                currentPos.x = (short)Math.Round(actualX);
                currentPos.z = (short)Math.Round(actualZ);
                currentPos.y = (short)Math.Round(actualY);
                plant.Add(new VoxelItem(currentPos.x, currentPos.y, currentPos.z, VoxelType.PLANT));
            }
            
            // chance to generate seeds / flowers on top
            if (random.Next(100) < plantDef.seedsChance)
            {
                plant.Add(new VoxelItem(currentPos.x, (short)(currentPos.y + 1), currentPos.z, plantDef.voxelTypeSeeds));
                plant.Add(new VoxelItem(currentPos.x, (short)(currentPos.y + 2), currentPos.z, plantDef.voxelTypeSeeds));
            }
        }
        return plant.ToArray();
    }

    /**
     * Generates water lilly plant model.
     * Creates a set of circular leaves around center of the plant and places
     * predefined flower on top of it.
     */
    public static VoxelItem[] GenerateWaterLily(WaterLilyDefinition plantDef)
    {
        List<Vector2Short> directions =
        [
            new (1, 0),
            new (-1, 0),
            new (0, 1),
            new (0, -1)
        ];
        
        List<VoxelItem> plant = [];
        Random random = new Random(plantDef.seed + plantDef.id);
        
        // select random number of leaves
        int totalLeaves = random.Next(plantDef.minLeaves, plantDef.maxLeaves);
        if (totalLeaves > 4)
            totalLeaves = 4; // limit to 4
        
        // place central stem
        plant.Add(new VoxelItem(0, 0, 0, plantDef.voxelTypeLeaves));
        
        // generate leaves in directions
        for (int i = 0; i < totalLeaves; i++)
        {
            int leafRadius = random.Next(plantDef.minLeafRadius, plantDef.maxLeafRadius);
            Vector2Short dir = directions[i];
            
            // calculate leaf center position offset by leafRadius in the given direction
            Vector2Short leafCenter = new Vector2Short((short)(dir.x * leafRadius), (short)(dir.y * leafRadius));
            
            // generate circular leaf around this center
            GenerateCircularLeaf(plant, leafCenter, leafRadius, plantDef.voxelTypeLeaves, plantDef.voxelTypeLeavesAlt);
        }
        
        // generate flower on top
        // generate flower bed
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                plant.Add(new VoxelItem((short)x, 1, (short)z, plantDef.voxelTypeFlowerOuter));
            }
        }
        
        // central inner part
        plant.Add(new VoxelItem(0, 2, 0, plantDef.voxelTypeFlowerInner));
        // petals
        plant.Add(new VoxelItem(1, 2, 1, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(-1, 2, 1, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(1, 2, -1, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(-1, 2, -1, plantDef.voxelTypeFlowerOuter));
        // further corners
        plant.Add(new VoxelItem(2, 2, 0, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(-2, 2, 0, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(0, 2, 2, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(0, 2, -2, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(2, 1, 0, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(-2, 1, 0, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(0, 1, 2, plantDef.voxelTypeFlowerOuter));
        plant.Add(new VoxelItem(0, 1, -2, plantDef.voxelTypeFlowerOuter));

        return plant.ToArray();
    }
    
    /**
     * Generates a circular leaf at specified center position with given radius.
     * Edge of the leaf uses alternative voxel type.
     */
    private static void GenerateCircularLeaf(List<VoxelItem> plant, Vector2Short center, int radius, VoxelType leafType, VoxelType leafTypeAlt)
    {
        // generate a circular leaf at height 0 centered at the given position
        int radiusSquared = radius * radius;
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                // check if point is within the circle
                int distSquared = x * x + z * z;
                
                if (distSquared <= radiusSquared)
                {
                    // add voxel at the leaf center offset
                    int worldX = center.x + x;
                    int worldZ = center.y + z;
                    
                    // use alternate leaf type for leaf edge
                    VoxelType voxelType = (distSquared > radiusSquared * 0.5f) ? leafTypeAlt : leafType;
                    
                    plant.Add(new VoxelItem((short)worldX, 0, (short)worldZ, voxelType));
                }
            }
        }
    }

    /**
     * Bush branch structure storing positions of branch tips and their direction of growth.
     */
    private struct BushBranch(float posX, float posZ, float dirX, float dirZ)
    {
        public readonly float posX = posX;
        public readonly float posZ = posZ;
        public readonly float dirX = dirX;
        public readonly float dirZ = dirZ;
    }

    /**
     * Generates a complex flowering bush.
     * Uses an expanding tree structure of branches that always grow upwards and have randomized XZ growth directions.
     * Each branch can spawn leaf and flower voxels around itself in its 4 neighbors in XZ plane.
     * The chances of branching, spawning leaves, flowers, etc. change throughout the generation process based on
     * current height.
     */
    public static VoxelItem[] GenerateBush(PlantBushDefinition plantDef)
    {
        List<VoxelItem> plant = [];
        Random random = new Random(plantDef.seed + plantDef.id);
        int splitChance = plantDef.splitChance;
        int branchEndChance = plantDef.initialBranchEndChance;
        int currentChanceLeaf = plantDef.initialLeafChance;
        int currentChanceFlower = plantDef.initialFlowerChance;
        int yLevel = 0; // current height level of the bush

        List<BushBranch> activeBranchEnds = [];
        // create initial branch
        float stemDx = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
        float stemDz = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
        activeBranchEnds.Add(new BushBranch(0f, 0f, stemDx, stemDz));
        
        // process active branches
        while (activeBranchEnds.Count > 0 && yLevel < plantDef.maxHeight)
        {
            List<BushBranch> newBranchEnds = [];
            // process each branch
            foreach (BushBranch branch in activeBranchEnds)
            {
                // place trunk/stem voxel at current branch position
                int branchX = (int)Math.Round(branch.posX);
                int branchZ = (int)Math.Round(branch.posZ);
                plant.Add(new VoxelItem((short)branchX, (short)yLevel, (short)branchZ, plantDef.trunkVoxelType));
                
                // attempt to generate leaves and flowers in 4 neighbor positions
                AddLeavesFlowersAroundBushBranch(plant, branch, yLevel, random, plantDef, currentChanceLeaf, currentChanceFlower);
                
                // determine how branch continues
                int branchChoice = random.Next(1000);
                
                if (branchChoice < branchEndChance)
                {
                    // branch ends - place a leaf voxel on top
                    plant.Add(new VoxelItem((short)branchX, (short)(yLevel + 1), (short)branchZ, plantDef.leafVoxelType));
                }
                else if (branchChoice < branchEndChance + splitChance)
                {
                    // branch splits into multiple new branches
                    int splitCount = random.Next(plantDef.splitIntoCountMin, plantDef.splitIntoCountMax + 1);
                    for (int i = 0; i < splitCount; i++)
                    {
                        // generate new direction for each new branch
                        float newDx = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
                        float newDz = random.NextSingle() * (plantDef.stemDxMax - plantDef.stemDxMin) + plantDef.stemDxMin;
                        newBranchEnds.Add(new BushBranch(branch.posX + branch.dirX, branch.posZ + branch.dirZ, newDx, newDz));
                    }
                }
                else
                {
                    // branch continues growing in its current direction
                    newBranchEnds.Add(new BushBranch(branch.posX + branch.dirX, branch.posZ + branch.dirZ, branch.dirX, branch.dirZ));
                }
            }

            // update active branches, chances
            activeBranchEnds = newBranchEnds;
            
            yLevel++;
            currentChanceLeaf += plantDef.leafChanceChange;
            currentChanceFlower += plantDef.flowerChanceChange;
            branchEndChance += plantDef.branchEndChanceChange;
        }
        
        // add leaf voxels on top of remaining active branches
        foreach (BushBranch branch in activeBranchEnds)
        {
            int branchX = (int)Math.Round(branch.posX);
            int branchZ = (int)Math.Round(branch.posZ);
            plant.Add(new VoxelItem((short)branchX, (short)yLevel, (short)branchZ, plantDef.leafVoxelType));
        }

        return plant.ToArray();
    }

    /**
     * Attempts to generate leaf and flower voxels around branch at current y position
     */
    private static void AddLeavesFlowersAroundBushBranch(List<VoxelItem> plant, BushBranch branch, int yLevel, Random random, PlantBushDefinition plantBushDefinition, int currentChanceLeaf, int currentChanceFlower)
    {
        // 4 neighbor positions in XZ plane
        Vector2Short[] neighbors =
        [
            new (1, 0),
            new (-1, 0),
            new (0, 1),
            new (0, -1),
        ];

        int branchX = (int)Math.Round(branch.posX);
        int branchZ = (int)Math.Round(branch.posZ);

        // attempt to generate leaves / flowers in neighbor directions
        foreach (Vector2Short neighbor in neighbors)
        {
            int neighborX = branchX + neighbor.x;
            int neighborZ = branchZ + neighbor.y;
            
            // check flower chance first
            int leafFlowerChoice = random.Next(1000);
            if (leafFlowerChoice < currentChanceFlower)
            {
                plant.Add(new VoxelItem((short)neighborX, (short)yLevel, (short)neighborZ, plantBushDefinition.flowerVoxelType));
            }
            else if (leafFlowerChoice < currentChanceFlower + currentChanceLeaf)
            {
                plant.Add(new VoxelItem((short)neighborX, (short)yLevel, (short)neighborZ, plantBushDefinition.leafVoxelType));
            }
        }
    }
}
