// FILE: ModelService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains ModelService class that encapsulates parallel generation of models from
//          supplied model definitions.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Overall classification of vegetation.
 */
public enum VegetationType : byte
{
    TREE,
    BUSH,
    GRASS,
    PLANT,
    REED,
    WATER_LILLY
}

/**
 * Encapsulating structure for all generated vegetation models.
 */
public class Vegetation(
    VegetationType type,
    VoxelItem[] voxels,
    TreeSize size = TreeSize.MEDIUM,
    TreeType treeType = TreeType.DECIDUOUS,
    TreeState state = TreeState.HEALTHY,
    short offsetY = 0,
    int id = 0)
{
    public readonly VegetationType type = type;
    public readonly TreeSize size = size;
    public readonly TreeType treeType = treeType;
    public readonly TreeState state = state;
    public readonly VoxelItem[] voxels = voxels;
    public readonly short offsetY = offsetY;
    public readonly int id = id;
}

/**
 * Overall type of model for the generation process.
 */
public enum ModelType : byte
{
    TREE,
    BUSH,
    GRASS,
    PLANT,
    PLANT_BUSH,
    REED,
    WATER_LILLY,
    ROCK,
    TRUNK,
    STALACTITE,
}

/**
 * Task for the parallel model generation service.
 */
public struct ModelGenerationTask(ModelType modelType, object definition)
{
    public readonly ModelType modelType = modelType;
    public readonly object definition = definition;
}

/**
 * Parallel generator of all used voxel models.
 * Model generation is a one time process only. All definitions must be supplied beforehand.
 * Then the service processes all in parallel and signals completion of all tasks.
 */
public class ModelService : IDisposable
{
    private readonly int seed;
    private readonly int maxThreads;
    private int currentSeedOffset; // ensures correct seed offset for each model
    private double startTime;
    
    private Action onModelsGenerated; // signal to root
    
    private readonly List<TreeDefinition> treeDefinitions;
    private readonly List<TreeDefinition> bushDefinitions;
    public ConcurrentBag<Vegetation> treeBushModels { get; } 
    
    private readonly List<GrassDefinition> grassDefinitions;
    public ConcurrentBag<Vegetation> grassModels { get; }
    
    private readonly List<PlantDefinition> plantDefinitions;
    private readonly List<PlantBushDefinition> plantBushDefinitions;
    public ConcurrentBag<Vegetation> plantModels { get; } // contains all generated land plants (no trees and bushes)
    
    private readonly List<ReedDefinition> reedDefinitions;
    public ConcurrentBag<Vegetation> reedModels { get; }
    
    private readonly List<WaterLilyDefinition> waterLilyDefinitions;
    public ConcurrentBag<Vegetation> waterLilyModels { get; }
    
    private readonly List<RockDefinition> rockDefinitions;
    public ConcurrentBag<Rock> rockModels { get; }
    
    private readonly List<TrunkDefinition> trunkDefinitions;
    public ConcurrentBag<Trunk> trunkModels { get; }
    
    private readonly List<StalactiteDefinition> stalactiteDefinitions;
    public ConcurrentBag<Stalactite> stalactiteModels { get; }
    
    private readonly ConcurrentQueue<ModelGenerationTask> taskQueue;
    private readonly List<Thread> workerThreads;
    private int activeThreads;
    private readonly Lock threadLock;

    private int plantBushIdOffset = 100; // plants and plant bushes are mixed later on, makes sure that ids do not collide
    
    
    public ModelService(int seed, int maxThreads)
    {
        this.seed = seed;
        this.maxThreads = maxThreads;
        
        taskQueue = new ConcurrentQueue<ModelGenerationTask>();
        workerThreads = [];
        threadLock = new Lock();
        
        // definitions
        treeDefinitions = [];
        bushDefinitions = [];
        grassDefinitions = [];
        plantDefinitions = [];
        reedDefinitions = [];
        waterLilyDefinitions = [];
        plantBushDefinitions = [];
        rockDefinitions = [];
        trunkDefinitions = [];
        stalactiteDefinitions = [];
        // models
        treeBushModels = [];
        rockModels = [];
        grassModels = [];
        plantModels = [];
        trunkModels = [];
        stalactiteModels = [];
        reedModels = [];
        waterLilyModels = [];
    }

    /**
     * Adds and configures tree definition
     */
    public void AddTreeDefinition(TreeDefinition treeDefinition)
    {
        treeDefinition.seed = seed + currentSeedOffset++;
        treeDefinition.id = treeDefinitions.Count;
        treeDefinitions.Add(treeDefinition);
    }
    
    /**
     * Adds and configures bush definition
     */
    public void AddBushDefinition(TreeDefinition bushDefinition)
    {
        bushDefinition.seed = seed + currentSeedOffset++;
        bushDefinition.id = bushDefinitions.Count;
        bushDefinitions.Add(bushDefinition);
    }
    
    /**
     * Adds and configures grass definition
     */
    public void AddGrassDefinition(GrassDefinition grassDefinition)
    {
        grassDefinition.seed = seed + currentSeedOffset++;
        grassDefinition.id = grassDefinitions.Count;
        grassDefinitions.Add(grassDefinition);
    }
    
    /**
     * Adds and configures plant definition
     */
    public void AddPlantDefinition(PlantDefinition plantDefinition)
    {
        plantDefinition.seed = seed + currentSeedOffset++;
        plantDefinition.id = plantDefinitions.Count;
        plantDefinitions.Add(plantDefinition);
    }
    
    /**
     * Adds and configures plant bush definition
     */
    public void AddPlantBushDefinition(PlantBushDefinition plantBushDefinition)
    {
        plantBushDefinition.seed = seed + currentSeedOffset++;
        plantBushDefinition.id = plantBushDefinitions.Count + plantBushIdOffset;
        plantBushDefinitions.Add(plantBushDefinition);
    }
    
    /**
     * Adds and configures reed definition
     */
    public void AddReedDefinition(ReedDefinition reedDefinition)
    {
        reedDefinition.seed = seed + currentSeedOffset++;
        reedDefinition.id = reedDefinitions.Count;
        reedDefinitions.Add(reedDefinition);
    }
    
    /**
     * Adds and configures water lily definition
     */
    public void AddWaterLilyDefinition(WaterLilyDefinition waterLilyDefinition)
    {
        waterLilyDefinition.seed = seed + currentSeedOffset++;
        waterLilyDefinition.id = waterLilyDefinitions.Count;
        waterLilyDefinitions.Add(waterLilyDefinition);
    }
    
    /**
     * Adds and configures rock definition
     */
    public void AddRockDefinition(RockDefinition rockDefinition)
    {
        rockDefinition.seed = seed + currentSeedOffset++;
        rockDefinition.id = rockDefinitions.Count;
        rockDefinitions.Add(rockDefinition);
    }
    
    /**
     * Adds and configures trunk definition
     */
    public void AddTrunkDefinition(TrunkDefinition trunkDefinition)
    {
        trunkDefinition.treeDefinition.seed = seed + currentSeedOffset++;
        trunkDefinition.treeDefinition.id = trunkDefinitions.Count;
        trunkDefinitions.Add(trunkDefinition);
    }
    
    /**
     * Adds and configures stalactite definition
     */
    public void AddStalactiteDefinition(StalactiteDefinition stalactiteDefinition)
    {
        stalactiteDefinition.seed = seed + currentSeedOffset++;
        stalactiteDefinition.id = stalactiteDefinitions.Count;
        stalactiteDefinitions.Add(stalactiteDefinition);
    }

    /**
     * Generates single tree / bush.
     * Saves generated model to treeBushModels.
     */
    private void GenerateTree(TreeDefinition treeDef, bool isBush = false)
    {
        TreeGenerator treeGen = new TreeGenerator(
            treeDef.nAttractionPoints,
            treeDef.maxIterations,
            treeDef.influenceRadius,
            treeDef.killDistance,
            treeDef.branchLength,
            treeDef.growthBias,
            treeDef.crownOffset,
            treeDef.envelopeType,
            treeDef.envelopeRadius,
            treeDef.envelopeHeight,
            treeDef.seed,
            treeDef.kTop,
            treeDef.kBottom,
            treeDef.radialBias,
            treeDef.verticalBias,
            treeDef.interiorMin,
            treeDef.coniferWedgeSpacing,
            treeDef.coniferWedgeHeight,
            treeDef.offsetY,
            treeDef.basicRadius,
            treeDef.radiusExponent
            );
        treeGen.Generate();
        VoxelItem[] treeVoxels = treeGen.GetVoxelModel(
            treeDef.trunk,
            treeDef.leaves,
            treeDef.padding,
            treeDef.maxLeafRadius,
            treeDef.leafRadiusScale,
            treeDef.maxLeafDispersion,
            treeDef.totalLeafPatchesForNode
        ).GetVoxelItems();

        Vegetation veg = new Vegetation(
            isBush ? VegetationType.BUSH : VegetationType.TREE,
            treeVoxels,
            treeDef.size,
            treeDef.type,
            treeDef.state,
            treeGen.offsetY,
            treeDef.id
        );
        
        treeBushModels.Add(veg);
    }

    /**
     * Generates single plant bush model (flowering bush).
     * Saves it into plantModels.
     */
    private void GeneratePlantBush(PlantBushDefinition plantBushDefinition)
    {
        VoxelItem[] plantVoxels = VegetationGenerator.GenerateBush(plantBushDefinition);
        Vegetation veg = new Vegetation(
            VegetationType.PLANT,
            plantVoxels,
            id:plantBushDefinition.id
        );
        plantModels.Add(veg);
    }
    
    /**
     * Generates single grass model.
     */
    private void GenerateGrass(GrassDefinition grassDef)
    {
        VoxelItem[] plantVoxels = VegetationGenerator.GenerateGrass(grassDef);
        
        Vegetation veg = new Vegetation(
            VegetationType.GRASS,
            plantVoxels,
            id:grassDef.id
        );
        
        grassModels.Add(veg);
    }

    /**
     * Generates single plant model. Encapsulates generation of all plants that use PlantDefinition structure.
     * Result is saved into plantModels.
     */
    private void GeneratePlant(PlantDefinition plantDef)
    {
        VoxelItem[] plantVoxels;
        switch (plantDef.plantType)
        {
            case PLANT_TYPE.POPPY:
                plantVoxels = VegetationGenerator.GeneratePoppy(plantDef);
                break;
            case PLANT_TYPE.BLUEBELL:
                plantVoxels = VegetationGenerator.GenerateBluebell(plantDef);
                break;
            case PLANT_TYPE.DAISY:
                plantVoxels = VegetationGenerator.GenerateDaisy(plantDef);
                break;
            case PLANT_TYPE.DANDELION:
                plantVoxels = VegetationGenerator.GenerateDandelion(plantDef);
                break;
            case PLANT_TYPE.FERN:
                plantVoxels = VegetationGenerator.GenerateFern(plantDef);
                break;
            case PLANT_TYPE.BURDOCK:
                plantVoxels = VegetationGenerator.GenerateBurdock(plantDef);
                break;
            default:
                return;
        }
        
        Vegetation veg = new Vegetation(
            VegetationType.PLANT,
            plantVoxels,
            id:plantDef.id
        );
        
        plantModels.Add(veg);
    }

    /**
     * Generates single reed model.
     */
    private void GenerateReed(ReedDefinition reedDef)
    {
        VoxelItem[] plantVoxels = VegetationGenerator.GenerateReed(reedDef);
        Vegetation veg = new Vegetation(
            VegetationType.REED,
            plantVoxels,
            id:reedDef.id
        );
        reedModels.Add(veg);
    }

    /**
     * Generates single water lily model.
     */
    private void GenerateWaterLily(WaterLilyDefinition waterLilyDef)
    {
        VoxelItem[] plantVoxels = VegetationGenerator.GenerateWaterLily(waterLilyDef);
        Vegetation veg = new Vegetation(
            VegetationType.WATER_LILLY,
            plantVoxels,
            id:waterLilyDef.id
        );
        waterLilyModels.Add(veg);
    }

    /**
     * Generates single trunk model.
     */
    private void GenerateTrunk(TrunkDefinition trunkDef)
    {
        (VoxelItem[] trunkVoxels, short offsetY) = TrunkGenerator.Generate(trunkDef);
        Trunk trunk = new Trunk(trunkVoxels, offsetY, trunkDef.treeDefinition.id);
        trunkModels.Add(trunk);
    }

    /**
     * Generates single rock model.
     */
    private void GenerateRock(RockDefinition rockDef)
    {
        (VoxelItem[] rockVoxels, short offsetY) = RockGenerator.Generate(
            rockDef.seed,
            rockDef.sizeX,
            rockDef.sizeY,
            rockDef.sizeZ,
            rockDef.radiusMax
        );
        Rock rock = new Rock(rockVoxels, offsetY, rockDef.id);
        rockModels.Add(rock);
    }

    /**
     * Generates single stalactite / stalagmite model
     */
    private void GenerateStalactite(StalactiteDefinition stalactiteDef)
    {
        VoxelItem[] stalVoxels = StalactiteGenerator.Generate(stalactiteDef);
        Stalactite stalactite = new Stalactite(stalVoxels, stalactiteDef.growthDir, stalactiteDef.id);
        stalactiteModels.Add(stalactite);
    }
    
    /**
     * Worker function for each thread.
     * If there are any tasks left, takes it and generates given model.
     */
    private void WorkerThread()
    {
        try
        {
            while (true)
            {
                if (!taskQueue.TryDequeue(out ModelGenerationTask task))
                    break;

                double timeStart = Time.GetUnixTimeFromSystem();
                switch (task.modelType)
                {
                    case ModelType.TREE:
                        GenerateTree((TreeDefinition)task.definition);
                        break;
                    case ModelType.BUSH:
                        GenerateTree((TreeDefinition)task.definition, true);
                        break;
                    case ModelType.ROCK:
                        GenerateRock((RockDefinition)task.definition);
                        break;
                    case ModelType.GRASS:
                        GenerateGrass((GrassDefinition)task.definition);
                        break;
                    case ModelType.PLANT:
                        GeneratePlant((PlantDefinition)task.definition);
                        break;
                    case ModelType.REED:
                        GenerateReed((ReedDefinition)task.definition);
                        break;
                    case ModelType.WATER_LILLY:
                        GenerateWaterLily((WaterLilyDefinition)task.definition);
                        break;
                    case ModelType.PLANT_BUSH:
                        GeneratePlantBush((PlantBushDefinition)task.definition);
                        break;
                    case ModelType.TRUNK:
                        GenerateTrunk((TrunkDefinition)task.definition);
                        break;
                    case ModelType.STALACTITE:
                        GenerateStalactite((StalactiteDefinition)task.definition);
                        break;
                }

                double duration = Time.GetUnixTimeFromSystem() - timeStart;
                GD.Print($"Generated {task.modelType} model in {duration:F2} s");
            }
            
            lock (threadLock)
            {
                activeThreads--;
                if (activeThreads == 0)
                {
                    double totalDuration = Time.GetUnixTimeFromSystem() - startTime;
                    GD.Print($"All models generated in {totalDuration:F2} s");
                    onModelsGenerated?.Invoke();
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr(e.ToString());
        }
    }

    /**
     * Starts up model generation process.
     * Enqueues all model definitions and initializes all threads.
     */
    public void GenerateModels()
    {
        startTime = Time.GetUnixTimeFromSystem();
        foreach (var treeDef in treeDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.TREE, treeDef));
        }
        
        foreach (var treeDef in bushDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.BUSH, treeDef));
        }
        
        foreach (var rockDef in rockDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.ROCK, rockDef));
        }
        
        foreach (var grassDef in grassDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.GRASS, grassDef));
        }
        
        foreach (var plantDef in plantDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.PLANT, plantDef));
        }
        
        foreach (var reedDef in reedDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.REED, reedDef));
        }
        
        foreach (var waterLillyDef in waterLilyDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.WATER_LILLY, waterLillyDef));
        }
        
        foreach (var plantBushDef in plantBushDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.PLANT_BUSH, plantBushDef));
        }
        
        foreach (var trunkDef in trunkDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.TRUNK, trunkDef));
        }
        
        foreach (var stalactiteDef in stalactiteDefinitions)
        {
            taskQueue.Enqueue(new ModelGenerationTask(ModelType.STALACTITE, stalactiteDef));
        }
        
        lock (threadLock)
        {
            activeThreads = Math.Min(maxThreads, taskQueue.Count);
        }

        workerThreads.Clear();
        for (int i = 0; i < activeThreads; i++)
        {
            var thread = new Thread(WorkerThread);
            workerThreads.Add(thread);
            thread.Start();
        }
    }
    
    public uint GetTotalModelsCount()
    {
        return (uint)(treeDefinitions.Count + bushDefinitions.Count + rockDefinitions.Count +
                grassDefinitions.Count + plantDefinitions.Count + reedDefinitions.Count +
                waterLilyDefinitions.Count + plantBushDefinitions.Count + trunkDefinitions.Count +
                stalactiteDefinitions.Count);
    }

    public uint GetRemainingTasksCount()
    {
        return (uint)taskQueue.Count + (uint)activeThreads;
    }

    /**
     * Subscribes given handler to this service onModelsGenerated signal.
     */
    public void SubscribeOnModelsGenerated(Action handler)
    {
        onModelsGenerated += handler;
    }

    /**
     * Waits for closure of all threads, cleans up storage.
     */
    public void Dispose()
    {
        foreach (Thread thread in workerThreads)
        {
            if (thread.IsAlive)
                thread.Join();
        }
        
        treeDefinitions.Clear();
        bushDefinitions.Clear();
        rockDefinitions.Clear();
        grassDefinitions.Clear();
        plantDefinitions.Clear();
        reedDefinitions.Clear();
        waterLilyDefinitions.Clear();
        plantBushDefinitions.Clear();
        trunkDefinitions.Clear();
        stalactiteDefinitions.Clear();
        
        treeBushModels.Clear();
        rockModels.Clear();
        grassModels.Clear();
        plantModels.Clear();
        reedModels.Clear();
        waterLilyModels.Clear();
        trunkModels.Clear();
        stalactiteModels.Clear();
    }
}
