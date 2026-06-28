// FILE: WorldGeneratorService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains service class that manages procedural generation of world regions and their unloading.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Structure for passing voxel models to world generator.
 */
public struct WorldGeneratorModels
{
    public ConcurrentBag<Vegetation> treeBushModels { get; init; }
    public ConcurrentBag<Vegetation> grassModels { get; init; }
    public ConcurrentBag<Vegetation> plantModels { get; init; }
    public ConcurrentBag<Vegetation> reedModels { get; init; }
    public ConcurrentBag<Vegetation> waterLilyModels { get; init; }
    public ConcurrentBag<Rock> rockModels { get; init; }
    public ConcurrentBag<Trunk> trunkModels { get; init; }
}

/**
 * Types of tasks supported by world generator service.
 */
public enum REGION_TASK_TYPE : byte
{
    GENERATE_TERRAIN,
    GENERATE_FEATURES
}

/**
 * Represents single task for the world generation service to process.
 */
public class WorldGenTask(REGION_TASK_TYPE type, int regionX, int regionZ, int priority)
{
    public readonly REGION_TASK_TYPE type = type;
    public readonly int regionX = regionX;
    public readonly int regionZ = regionZ;
    public readonly int priority = priority;
}

/**
 * Manages two-step (terrain, features) generation of world regions and their unloading.
 * Uses world generator to generate both terrain and feature steps of the world region generation process.
 * Its behavior is controlled by task system that is managed from root. Generation tasks are processed based
 * on specified priority and in parallel with up to the specified number of threads.
 * Whereas unloading of world regions happens immediately inside the UnloadRegion call.
 */
public class WorldGeneratorService : IDisposable
{
    private readonly WorldGenerator worldGenerator;
    private readonly ushort regionSizeInTerrainUnits;
    private readonly int columnsInRegionOneAxis;
    private readonly int metersPerRegion;
    // generated and currently being generated regions in both phases.
    private readonly ConcurrentDictionary<(int, int), WorldRegion> activeRegions;
    private readonly PriorityQueue<WorldGenTask, int> taskQueue; // queue of tasks that can be processed
    private readonly List<WorldGenTask> pendingFeatureTasks; // feature phase tasks that are waiting for their terrain step
    // or neighbor region generation
    
    private readonly Lock taskQueueLock; // lock for access to task lists and queues
    private readonly Lock eventLock;

    private Action<WorldRegion> onRegionGenerated; // fires when region is fully generated
    private int activeThreads;
    private readonly List<Thread> workerThreads;
    private readonly ManualResetEventSlim taskAvailable; // signals availability of tasks, to startup threads
    private volatile bool stopWorkers;
    
    public WorldGeneratorService(int threads, WorldGeneratorParams worldGenParams)
    {
        worldGenerator = new WorldGenerator(worldGenParams, this);
        activeRegions = new ConcurrentDictionary<(int, int), WorldRegion>();
        taskQueue = new  PriorityQueue<WorldGenTask, int>();
        pendingFeatureTasks = [];
        taskQueueLock = new Lock();
        eventLock = new Lock();
        workerThreads = [];
        taskAvailable = new ManualResetEventSlim(false);

        metersPerRegion = worldGenParams.metersPerRegion;
        regionSizeInTerrainUnits = (ushort)(metersPerRegion * worldGenParams.unitsPerMeter);
        columnsInRegionOneAxis = metersPerRegion / worldGenParams.metersPerChunk;
        
        // start worker threads
        for (int i = 0; i < threads; i++)
        {
            Thread thread = new Thread(WorkerThread)
            {
                IsBackground = true
            };
            workerThreads.Add(thread);
            thread.Start();
        }
    }
    
    /**
     * Returns region coordinates to which belongs given world position.
     */
    public (int regionX, int regionZ) WorldPosToRegionCoords(double worldX, double worldZ)
    {
        int regionX = (int)Math.Floor(worldX / metersPerRegion);
        int regionZ = (int)Math.Floor(worldZ / metersPerRegion);
        return (regionX, regionZ);
    }
    
    /**
     * Returns number of currently working threads.
     */
    public uint GetActiveThreadsCount()
    {
        return (uint)activeThreads;
    }

    /**
     * Returns number of tasks that are ready for processing.
     */
    public uint GetRemainingTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)taskQueue.Count;
        }
    }
    
    /**
     * Returns number of feature phase tasks that are waiting for completion of terrain step generation.
     */
    public uint GetLockedTasksCount()
    {
        lock (taskQueueLock)
        {
            return (uint)pendingFeatureTasks.Count;
        }
    }
    
    /**
     * Returns count of currently active regions.
     */
    public uint GetActiveRegionsCount()
    {
        return (uint)(activeRegions.Count - activeThreads);
    }

    /**
     * Passes encapsulated voxel models to world generator.
     */
    public void PassModelsToWorldGenerator(WorldGeneratorModels worldGenModels)
    {
        worldGenerator.SetupModels(worldGenModels);
    }
    
    /**
     * Returns world region with given coordinates if it is ready or null.
     */
    public WorldRegion GetRegionWhenReady(int regionX, int regionZ)
    {
        if (activeRegions.ContainsKey((regionX, regionZ)))
        {
            WorldRegion region = activeRegions[(regionX, regionZ)];
            if (region.ready)
            {
                return region;
            }
        }

        return null;
    }

    /**
     * Returns world region with given coordinates even if it is not finished.
     * Mainly for obtaining neighbor regions from world generator.
     */
    public WorldRegion GetRegion(int regionX, int regionZ)
    {
        if (activeRegions.ContainsKey((regionX, regionZ)))
        {
            return activeRegions[(regionX, regionZ)];;
        }

        return null;
    }
    
    /**
     * Checks whether given region and its 8 neighbors have generated terrain.
     */
    private bool CanRegionGenerateFeatures(int regionX, int regionZ)
    {
        for (int x = regionX - 1; x <= regionX + 1; x++)
        {
            for (int z = regionZ - 1; z <= regionZ + 1; z++)
            {
                if (!activeRegions.ContainsKey((x, z)))
                    return false;
                
                WorldRegion region = activeRegions[(x, z)];
                if (!region.firstStepDone) // terrain step needs to be done
                    return false;
            }
        }

        return true;
    }
    
    /**
     * Called after a region finishes its terrain generation phase.
     * Checks whether given regions terrain step completion unlocked any region
     * to move to the second step of generation.
     */
    private void OnRegionTerrainGenerated(WorldRegion region)
    {
        // check all pending feature generation tasks if they can be scheduled now
        List<WorldGenTask> tasksToSchedule = [];
        
        lock (taskQueueLock)
        {
            foreach (WorldGenTask task in pendingFeatureTasks)
            {
                if (CanRegionGenerateFeatures(task.regionX, task.regionZ))
                {
                    tasksToSchedule.Add(task);
                }
            }

            // schedule tasks that can be now generated and remove them from pending list
            foreach (WorldGenTask task in tasksToSchedule)
            {
                taskQueue.Enqueue(task, task.priority);
                pendingFeatureTasks.Remove(task);
                taskAvailable.Set();
            }
        }
    }

    /**
     * Immediately unloads region with given coordinates.
     * Removes it from active regions, ready queue and waiting list.
     * Process of currently being generated region is not stopped. But link to that region is removed.
     * Queue is rebuild without tasks for this region.
     */
    public void UnloadRegion(int regionX, int regionZ)
    {
        lock (taskQueueLock)
        {
            // remove it from active
            activeRegions.TryRemove((regionX, regionZ), out _);
            
            // remove it from pending
            WorldGenTask pendingTaskToRemove = null;
            foreach (WorldGenTask pendingTask in pendingFeatureTasks)
            {
                if (pendingTask.regionX == regionX && pendingTask.regionZ == regionZ)
                {
                    pendingTaskToRemove = pendingTask;
                    break;
                }
            }
            if (pendingTaskToRemove != null)
            {
                pendingFeatureTasks.Remove(pendingTaskToRemove);
            }
            
            // remove from task queue
            List<WorldGenTask> remainingTasks = [];
            taskAvailable.Reset();
            while (taskQueue.Count > 0)
            {
                WorldGenTask t = taskQueue.Dequeue();
                if (!(t.regionX == regionX && t.regionZ == regionZ))
                {
                    remainingTasks.Add(t);
                }
            }
            
            foreach (WorldGenTask t in remainingTasks)
            {
                taskQueue.Enqueue(t, t.priority);
            }

            if (taskQueue.Count > 0)
            {
                taskAvailable.Set();
            }
        }
    }

    /**
     * Adds generation task of either terrain or feature phase of generation.
     * Terrain generation can be scheduled immediately. But feature phase firstly needs completed terrain phase
     * in this region and its 8 neighbors.
     */
    public void AddTask(WorldGenTask task)
    {
        lock (taskQueueLock)
        {
            switch (task.type)
            {
                case REGION_TASK_TYPE.GENERATE_TERRAIN:
                    // terrain can be generated as is
                    taskQueue.Enqueue(task, task.priority);
                    taskAvailable.Set();
                    break;
                case REGION_TASK_TYPE.GENERATE_FEATURES:
                    // features can only be generated if terrain is generated for given region and its 8 neighbors,
                    // otherwise it needs to wait in list of pending feature generation tasks
                    if (CanRegionGenerateFeatures(task.regionX, task.regionZ))
                    {
                        taskQueue.Enqueue(task, task.priority);
                        taskAvailable.Set();
                    }
                    else
                    {
                        pendingFeatureTasks.Add(task);
                    }
                    break;
            }
        }
    }
    
    /**
     * Working loop of each thread.
     * Processes generation tasks of both world region generation phases.
     */
    private void WorkerThread()
    {
        while (!stopWorkers)
        {
            WorldGenTask task = null;
            WorldRegion region = null;
            bool hasTask = false;
            // obtain task and its region
            lock (taskQueueLock)
            {
                if (taskQueue.Count > 0)
                {
                    task = taskQueue.Dequeue();
                    switch (task.type)
                    {
                        case REGION_TASK_TYPE.GENERATE_TERRAIN:
                            if (activeRegions.ContainsKey((task.regionX, task.regionZ)))
                                continue; // already loaded, skip
                            
                            // otherwise prepare region
                            region = new WorldRegion(regionSizeInTerrainUnits, columnsInRegionOneAxis, task.regionX, task.regionZ);
                            activeRegions[(task.regionX, task.regionZ)] = region;
                            hasTask = true;
                            break;
                        case REGION_TASK_TYPE.GENERATE_FEATURES:
                            if (activeRegions.ContainsKey((task.regionX, task.regionZ)))
                            {
                                region = activeRegions[(task.regionX, task.regionZ)];
                                hasTask = true;
                            }

                            break;
                    }
                }
            }
            // process the task
            if (hasTask)
            {
                lock (eventLock)
                {
                    activeThreads++;
                }
                switch (task.type)
                {
                    case REGION_TASK_TYPE.GENERATE_TERRAIN:
                        worldGenerator.GenerateRegionTerrain(region);
                        GD.Print($"Terrain generated in region {region.posX}, {region.posZ}");
                        OnRegionTerrainGenerated(region);
                        break;
                    case REGION_TASK_TYPE.GENERATE_FEATURES:
                        worldGenerator.GenerateRegionFeatures(region);
                        GD.Print($"Features generated in region {region.posX}, {region.posZ}");
                        lock (eventLock)
                        {
                            onRegionGenerated?.Invoke(region); // signal that this region was generated
                        }
                        break;
                }
                lock (eventLock)
                {
                    activeThreads--;
                }
            }
            else
            {
                taskAvailable.Wait(100);
                taskAvailable.Reset();
            }
        }
    }

    /**
     * Waits for closure of all threads.
     */
    public void Dispose()
    {
        stopWorkers = true;
        taskAvailable.Set();
        foreach (Thread thread in workerThreads)
        {
            if (thread.IsAlive)
                thread.Join();
        }
        taskAvailable.Dispose();
    }

    /**
     * Given handler subscribes to this service onRegionGenerated signal.
     */
    public void SubscribeOnRegionGenerated(Action<WorldRegion> handler)
    {
        lock (eventLock)
        {
            onRegionGenerated += handler;
        }
    }
}
