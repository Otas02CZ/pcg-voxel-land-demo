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
 * Supports several weather types (clear, snow, rain, dust phenomena in caves), management of particle systems,
 * environmental volumetric fog, as well as day - night cycle.
 */
public class WeatherEnvironmentManager
{
    // dependencies
    private Player player;
    private WeatherParticleSystem particleSystem;
    private DirectionalLight3D sun;
    private DirectionalLight3D moon;
    private Environment environment;
    private ShaderMaterial skyMaterial;
    private WorldGeneratorService worldGeneratorService;
    private readonly int metersPerRegion;
    private readonly int terrainUnitsPerMeter;
    
    // general
    private ENV_TYPE currentEnvironment;
    private WEATHER_TYPE lastWeatherType;

    // weather simulation parameters
    private bool weatherCycleEnabled;
    private const float weatherSimDelta = 0.1f;
    private double lastWeatherSimUpdateTime;
    private double weatherSimTime;

    // thresholds
    private const float snowThreshold = 0.35f;
    private const float rainThreshold = 0.55f;
    private const float minFogDensity = 0.0f;
    private const float maxFogDensity = 0.03f;

    // fog linear transition
    private float currentFog;
    private float targetFog;
    private const float transitionDelta = 0.05f;
    
    // time of day / sun movement simulation
    private bool dayCycleEnabled;
    private float sunAngle; // current angle 0 - 2 PI
    private float moonAngle; // offset by PI
    private const float dayCycleSimDelta = 0.05f;
    private double lastDayCycleSimUpdateTime;
    private readonly float sunAngleDefault = Mathf.DegToRad(60);
    private readonly float fadeRad = Mathf.DegToRad(15); // angle range for sunrise / sunset fading
    private const float sunCycleSpeed = 0.04f; // sun rotation speed
    private const float nightCycleSpeedMultiplier = 1.6f; // multiplier of day cycle speed during night
    private const float sunOrbitRadius = 200.0f; // orbiting radius of the sun
    // limits
    private const float dayTimeMin = 0.01f;
    private const float dayTimeMax = 0.99f;
    private const float sunMaxLightEnergy = 1.0f;
    private const float moonMaxLightEnergy = 0.35f;

    // player positioning
    private const double debouncePlayerPos = 0.1D; // seconds
    private double lastPlayerPosUpdateTime;
    private Vector3Double playerPosition;
    private bool regionUnknown;
    private int playerRegionX;
    private int playerRegionZ;
    
    private readonly FastNoiseLite weatherNoise;

    public WeatherEnvironmentManager(Player player, DirectionalLight3D sun, DirectionalLight3D moon, WorldEnvironment worldEnvironment, int metersPerRegion, int terrainUnitsPerMeter)
    {
        this.player = player;
        this.sun = sun;
        this.moon = moon;
        this.environment = worldEnvironment.GetEnvironment();
        this.metersPerRegion = metersPerRegion;
        this.terrainUnitsPerMeter = terrainUnitsPerMeter;
        this.skyMaterial = (ShaderMaterial)environment.GetSky().GetMaterial();
        
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
        weatherCycleEnabled = true;
        dayCycleEnabled = false;
        regionUnknown = true;
        weatherSimTime = 0;
        sunAngle = sunAngleDefault;
        moonAngle = sunAngleDefault + Mathf.Pi;
        UpdateSunMoonPositions(weatherSimDelta); // initialize sun position and day-night cycle in shader
    }

    /**
     * Stops the simulation, cleans dependencies.
     */
    public void Stop()
    {
        worldGeneratorService = null;
        weatherCycleEnabled = false;
        dayCycleEnabled = false;
        particleSystem.SwitchParticleSystem(WEATHER_TYPE.CLEAR);
    }

    /**
     * Signalizes that a player has moved.
     * Updates player and current environment based on player vertical and horizontal position.
     */
    public void OnPlayerPositionChanged()
    {
        // reposition sun based on player position, if automatic sun cycle is disabled
        if (!dayCycleEnabled)
        {
            MoveLights();
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
        short playerHeight = (short)Math.Floor(playerPosition.y * terrainUnitsPerMeter);
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
        if (!regionUnknown)
        {
            return;
        }

        if (worldRegion.posX == playerRegionX && worldRegion.posZ == playerRegionZ)
        {
            UpdateEnvironment();
        }
    }
    
    /**
     * Processes simulation step of weather and day cycle in defined intervals (simulation delta).
     * Actual weather / day cycle simulation happens in dependent functions.
     */
    public void SimulationStep()
    {
        double currentTime = Time.GetUnixTimeFromSystem();
        
        // day cycle simulation
        if (dayCycleEnabled)
        {
            if (currentTime - lastDayCycleSimUpdateTime < dayCycleSimDelta)
            {
                return;
            }
            lastDayCycleSimUpdateTime = currentTime;
            UpdateSunMoonPositions(dayCycleSimDelta);
        }
        // weather cycle simulation
        if (weatherCycleEnabled)
        {
            if (currentTime - lastWeatherSimUpdateTime < weatherSimDelta)
            {
                return;
            }
            lastWeatherSimUpdateTime = currentTime;
            weatherSimTime += weatherSimDelta;
            SimulateWeather();
        }
    }

    /**
     * Processes a single simulation step (for current time values) of the weather simulation.
     * Determines current weather type. Updates particle systems, fog intensity and sky shader parameters.
     */
    private void SimulateWeather()
    {
        float noiseValue = weatherNoise.GetNoise1D((float)weatherSimTime) * 0.5f + 0.5f; // 0 - 1
        
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
        // apply weather in sky shader
        GD.Print($"Weather Intensity: {noiseValue}");
        skyMaterial.SetShaderParameter("weather_intensity", noiseValue);
    }

    /**
     * Updates sky shader day time based on current sun angle.
     */
    private void UpdateSkyDayTime()
    {
        // transform day cycle sun angle to day time value (0 midnight - 1 noon)
        float dayTime = Mathf.Sin(sunAngle) * 0.5f + 0.5f;
        dayTime = Mathf.Min(dayTime, dayTimeMax);
        dayTime = Mathf.Max(dayTime, dayTimeMin);
        GD.Print($"Day Time: {dayTime}"); 
        skyMaterial.SetShaderParameter("time_of_day", dayTime);
    }

    /**
     * Moves lights - sun and moon depending on their current angle.
     */
    public void MoveLights()
    {
        MoveLight(sun, sunAngle);
        MoveLight(moon, moonAngle);
    }
    
    /**
     * Moves given light to a new position based on current player position and sun angle
     * of rotation around the world.
     * If automatic sun-moon cycle is not enabled, this only repositions the sun to correctly move
     * with the player through the world.
     */
    private void MoveLight(DirectionalLight3D light, float lightAngle)
    {
        // get player position
        Vector3 playerPos = player.GlobalPosition;

        // calculate light position in circle around player
        // light moves in a vertical circle perpendicular to the ground
        float lightX = playerPos.X + Mathf.Cos(lightAngle) * sunOrbitRadius;
        float lightY = playerPos.Y + Mathf.Sin(lightAngle) * sunOrbitRadius;
        float lightZ = playerPos.Z;

        Vector3 lightPosition = new Vector3(lightX, lightY, lightZ);
        light.GlobalPosition = lightPosition;

        // light looks toward player position
        light.LookAt(playerPos, Vector3.Up);
    }
    
    /**
     * Simulates day time, moves sun and moon, changes their intensity.
     * Advances the sun/moon angle around the world (player).
     * Uses CalculateLightEnergy to change its energy value depending on its current angle of rotation.
     */
    private void UpdateSunMoonPositions(double delta)
    {
        // increment the angle based on time and speed
        if (sunAngle <= Mathf.Pi)
        {
            sunAngle += sunCycleSpeed * (float)delta;
            moonAngle += sunCycleSpeed * (float)delta;
        }
        else // night is a bit faster and shorter
        {
            sunAngle += sunCycleSpeed * nightCycleSpeedMultiplier * (float)delta;
            moonAngle += sunCycleSpeed * nightCycleSpeedMultiplier * (float)delta;
        }

        // keep angle in 0 to 2 PI range
        if (sunAngle >= Mathf.Tau)
        {
            sunAngle -= Mathf.Tau;
        }
        
        // keep angle in 0 to 2 PI range
        if (moonAngle >= Mathf.Tau)
        {
            moonAngle -= Mathf.Tau;
        }

        // move sun and moon to new position based on player position and updated angle
        MoveLights();

        UpdateSkyDayTime();

        // update sun energy
        sun.LightEnergy = CalculateLightEnergy(sunAngle, sunMaxLightEnergy);
        moon.LightEnergy = CalculateLightEnergy(moonAngle, moonMaxLightEnergy);
    }

    /**
     * Calculates light energy depending on its current angle:
     * Sun increases energy as it goes up 0 - fadeRad,
     * then stays at full energy until it reaches pi - fadeRad,
     * when it starts to reduce its energy as it goes down.
     * Its energy is zero when it is below the horizon.
     * The angle is then applied in MoveSun method.
     * The same applies to the moon, but as it is offset by PI,
     * its light is dimm/lit at different times.
     */
    private float CalculateLightEnergy(float lightAngle, float maxValue)
    {
        // new light energy value
        float lightEnergy;

        // upper half, light is above the horizon
        if (lightAngle <= Mathf.Pi)
        {
            // 0 - fadeRad, light rising
            if (lightAngle < fadeRad)
            {
                // fade energy in
                lightEnergy = lightAngle / fadeRad;
            }
            // (PI - fadeRad) - PI, light going down
            else if (lightAngle > Mathf.Pi - fadeRad)
            {
                // fade energy out
                lightEnergy = (Mathf.Pi - lightAngle) / fadeRad;
            }
            // middle, full brightness
            else
            {
                lightEnergy = 1.0f;
            }
        }
        // lower half, light below the horizon, dimmed
        else
        {
            lightEnergy = 0.0f;
        }

        lightEnergy *= maxValue; // scale in maximum range

        return lightEnergy;
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

    /**
     * Toggles day cycle simulation.
     */
    public void ToggleDayCycle()
    {
        dayCycleEnabled = !dayCycleEnabled;
    }

    /**
     * Toggles weather cycle simulation.
     */
    public void ToggleWeatherSimulation()
    {
        weatherCycleEnabled = !weatherCycleEnabled;
    }
}
