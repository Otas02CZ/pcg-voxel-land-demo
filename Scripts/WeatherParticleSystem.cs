// FILE: WeatherParticleSystem.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Management script for weather particle systems.

using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Describes one of available weather particle systems, or none.
 */
public enum PART_SYS : byte
{
    NONE,
    SNOW,
    RAIN,
    DUST
}

/**
 * Manages weather particle systems, their emission and amount ratios (intensity of given particle system).
 */
public partial class WeatherParticleSystem : Node3D
{
    // particle system nodes
    private GpuParticles3D snowPartSys;
    private GpuParticles3D rainPartSys;
    private GpuParticles3D dustPartSys;
    // current one
    private GpuParticles3D currentPartSysNode;
    private PART_SYS currentPartSys = PART_SYS.NONE;
    
    public override void _Ready()
    {
        snowPartSys = GetNode<GpuParticles3D>("Snow");
        rainPartSys = GetNode<GpuParticles3D>("Rain");
        dustPartSys = GetNode<GpuParticles3D>("Dust");
        
        snowPartSys.SetEmitting(false);
        rainPartSys.SetEmitting(false);
        dustPartSys.SetEmitting(false);
    }

    /**
     * Switches to specified particle system or stops particles if none.
     */
    public void SwitchParticleSystem(PART_SYS newSystem)
    {
        if (currentPartSysNode != null)
        {
            currentPartSysNode.SetEmitting(false);
            currentPartSysNode = null;
        }
        
        currentPartSys = newSystem;

        switch (newSystem)
        {
            case PART_SYS.SNOW:
                currentPartSysNode = snowPartSys;
                break;
            case PART_SYS.RAIN:
                currentPartSysNode = rainPartSys;
                break;
            case PART_SYS.DUST:
                currentPartSysNode = dustPartSys;
                break;
        }

        if (currentPartSysNode != null)
        {
            currentPartSysNode.SetEmitting(true);
        }
    }

    /**
     * Updates intensity of current particle system.
     * Expects values between 0 and 1.
     */
    public void UpdateIntensity(float intensity)
    {
        if (currentPartSysNode != null)
        {
            currentPartSysNode.SetAmountRatio(intensity);
        }
    }
}
