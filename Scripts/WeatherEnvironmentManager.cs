// FILE: WeatherEnvironmentManager.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Management script for procedural system of weather and day-night cycle.

using Godot;
using System;
using Environment = Godot.Environment;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents types of environments the weather - env system works with.
 */
public enum ENV_TYPE : byte
{
    NORMAL,
    SNOW,
    CAVES
}

/**
 * Represents types of weather / phenomena the system can deliver.
 */
public enum WEATHER_TYPE : byte
{
    CLEAR,
    SNOW,
    RAIN,
    DUST
}

/**
 * System for management weather and environment.
 * Simulates weather with a 1D procedural noise and environment information obtained from world regions
 * based on current player position.
 * Supports several weather types (clear, snow, rain, dust phenomena in caves), management of particle systems and environment fog.
 * TODO: sky cloud intensity and color, day-night cycle - sky and sun/moon (movement and intesity)
 */
public class WeatherEnvironmentManager
{
    // dependencies
    private Player player;
    private WeatherParticleSystem particleSystem;
    private DirectionalLight3D sun;
    private Environment environment;
    private WorldGeneratorService worldGeneratorService;

    private readonly int metersPerRegion;
    private readonly int terrainUnitsPerMeter;

    // main parameters and states
    private bool running;
    private const float simulationDelta = 0.1f;
    private double lastSimulationUpdateTime;
    private double simulationTime;
    private ENV_TYPE currentEnvironment;
    private WEATHER_TYPE lastWeatherType;

    // thresholds
    private const float snowThreshold = 0.35f;
    private const float rainThreshold = 0.55f;
    private const float minFogDensity = 0.0f;
    private const float maxFogDensity = 0.2f;

    // fog linear transition
    private float currentFog;
    private float targetFog;
    private const float transitionDelta = 0.05f;

    // player positioning
    private const double debouncePlayerPos = 0.1D; // milliseconds
    private double lastPlayerPosUpdateTime;
    private Vector3Double playerPosition;
    private bool regionUnknown;
    private int playerRegionX;
    private int playerRegionZ;
    private const int caveOffsetTerrainUnits = 0;
    
    private readonly FastNoiseLite weatherNoise;

    public WeatherEnvironmentManager(Player player, DirectionalLight3D sun, WorldEnvironment worldEnvironment, int metersPerRegion, int terrainUnitsPerMeter)
    {
        this.player = player;
        this.sun = sun;
        this.environment = worldEnvironment.GetEnvironment();
        this.metersPerRegion = metersPerRegion;
        this.terrainUnitsPerMeter = terrainUnitsPerMeter;
        
        particleSystem = player.GetParticleSystem();
        player.SubscribeOnPlayerPositionChanged(OnPlayerPositionChanged);
        
        weatherNoise = new FastNoiseLite();
        weatherNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
        weatherNoise.SetFrequency(0.008f);
        weatherNoise.SetFractalOctaves(4);
        weatherNoise.SetFractalType(FastNoiseLite.FractalTypeEnum.Fbm);
        weatherNoise.SetFractalLacunarity(2.0f);
        weatherNoise.SetFractalGain(0.6f);
    }

    /**
     * Starts and initializes the weather system.
     */
    public void Start(WorldGeneratorService worldGeneratorService, int seed)
    {
        this.worldGeneratorService = worldGeneratorService;
        worldGeneratorService.SubscribeOnRegionGenerated(OnRegionGenerated);
        weatherNoise.SetSeed(seed);
        particleSystem.SwitchParticleSystem(WEATHER_TYPE.CLEAR);
        currentEnvironment = ENV_TYPE.NORMAL;
        lastWeatherType = WEATHER_TYPE.CLEAR;
        running = true;
        regionUnknown = true;
        simulationTime = 0;
    }

    /**
     * Stops the simulation, cleans dependencies.
     */
    public void Stop()
    {
        worldGeneratorService = null;
        running = false;
        particleSystem.SwitchParticleSystem(WEATHER_TYPE.CLEAR);
    }

    /**
     * Signalizes that a player has moved.
     * Updates player and current environment based on player vertical and horizontal position.
     */
    public void OnPlayerPositionChanged()
    {
        if (!running)
        {
            return;
        }
        // debounce
        double currentTime = Time.GetUnixTimeFromSystem();
        if (currentTime - lastPlayerPosUpdateTime < debouncePlayerPos)
        {
            return;
        }
        lastPlayerPosUpdateTime = currentTime;
        // obtain player and their world region position
        playerPosition = player.GetRealPosition();
        (playerRegionX, playerRegionZ) = worldGeneratorService.WorldPosToRegionCoords(playerPosition.x, playerPosition.z);
        
        UpdateEnvironment();
    }

    /**
     * Updates current environment based on current player position.
     * Uses WorldRegion information to classify player position to a set of supported environments.
     */
    private void UpdateEnvironment()
    {
        // obtain world region
        WorldRegion region = worldGeneratorService.GetRegion(playerRegionX, playerRegionZ);
        if (region == null || !region.ready)
        {
            regionUnknown = true;
            return; // not ready yet
        }
        regionUnknown = false;
        // calculate local region coordinates and height
        ushort playerRegionLocalX = (ushort)Math.Floor((playerPosition.x - playerRegionX * metersPerRegion) * terrainUnitsPerMeter);
        ushort playerRegionLocalZ = (ushort)Math.Floor((playerPosition.z - playerRegionZ * metersPerRegion) * terrainUnitsPerMeter);
        short playerHeight = (short)Math.Floor(playerPosition.y * terrainUnitsPerMeter - caveOffsetTerrainUnits);
        // determine environment type
        if (playerHeight < region.GetHeightAtLocalCoords(playerRegionLocalX, playerRegionLocalZ))
        {
            currentEnvironment = ENV_TYPE.CAVES;
        } else if (region.GetSnowAtLocalCoords(playerRegionLocalX, playerRegionLocalZ) > 0)
        {
            currentEnvironment = ENV_TYPE.SNOW;
        } else
        {
            currentEnvironment = ENV_TYPE.NORMAL;
        }
    }

    /**
     * Signalizes that a region was generated.
     * Used to re-sync the system if waiting on region where the player is positioned
     */
    public void OnRegionGenerated(WorldRegion worldRegion)
    {
        if (!running && !regionUnknown)
        {
            return;
        }

        if (worldRegion.posX == playerRegionX && worldRegion.posZ == playerRegionZ)
        {
            UpdateEnvironment();
        }
    }
    
    /**
     * Processes simulation step in defined intervals (simulation delta).
     * Determines current weather type.
     * Updates particle systems and fog intensity.
     * TODO: sky cloud intensity and color, day-night cycle - sky and sun/moon (movement and intesity)
     */
    public void SimulationStep()
    {
        // ensure simulation intervals
        double currentTime = Time.GetUnixTimeFromSystem();
        if (currentTime - lastSimulationUpdateTime < simulationDelta)
        {
            return;
        }
        lastSimulationUpdateTime = currentTime;
        simulationTime += simulationDelta;
        float noiseValue = weatherNoise.GetNoise1D((float)simulationTime) * 0.5f + 0.5f; // 0 - 1
        
        // determine new weather state
        WEATHER_TYPE newWeatherType = WEATHER_TYPE.CLEAR;
        float particleSystemIntensity = 0;
        
        switch (currentEnvironment)
        {
            // dust in caves and deserts happens always directly based on noise value
            case ENV_TYPE.CAVES:
                newWeatherType = WEATHER_TYPE.DUST;
                particleSystemIntensity = noiseValue;
                break;
            // snowing only above snow threshold
            case ENV_TYPE.SNOW:
                if (noiseValue <= snowThreshold)
                {
                    newWeatherType = WEATHER_TYPE.CLEAR;
                    break;
                }
                
                newWeatherType = WEATHER_TYPE.SNOW;
                particleSystemIntensity = (noiseValue - snowThreshold) * (1 / (1 - snowThreshold)); // values above threshold scaled 0-1
                break;
            // raining only above rain threshold
            case ENV_TYPE.NORMAL:
                if (noiseValue <= rainThreshold)
                {
                    newWeatherType = WEATHER_TYPE.CLEAR;
                    break;
                }
                
                newWeatherType = WEATHER_TYPE.RAIN;
                particleSystemIntensity = (noiseValue - rainThreshold) * (1 / (1 - rainThreshold)); // values above threshold scaled 0-1
                break;
        }
        
        // determine volumetric fog
        if (newWeatherType == WEATHER_TYPE.DUST)
        {
            UpdateFogTarget(0);
        }
        else
        {
            UpdateFogTarget(minFogDensity + particleSystemIntensity * (maxFogDensity - minFogDensity));
        }

        if (newWeatherType != lastWeatherType)
        {
            lastWeatherType = newWeatherType;
            particleSystem.SwitchParticleSystem(newWeatherType);
        }

        if (newWeatherType != WEATHER_TYPE.CLEAR)
        {
            particleSystem.UpdateIntensity(particleSystemIntensity);
        }
        // apply fog transition
        LinearTransitionFog();
    }

    /**
     * Updates fog target.
     */
    private void UpdateFogTarget(float newFogValue)
    {
        targetFog = newFogValue;
    }

    /**
     * Transitions fog with linear interpolation based on current and target fog value.
     */
    private void LinearTransitionFog()
    {
        currentFog += (targetFog - currentFog) * transitionDelta;
        environment.SetVolumetricFogDensity(currentFog);
    }
}
