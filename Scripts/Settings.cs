// FILE: Settings.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the Settings GUI, including necessary internal structures

using Godot;
using System;
using System.Text.Json;

namespace PCGVoxelLandscapes.Scripts;

/**
 * MSAA configuration enum
 */
public enum MSAA_SETTINGS : byte
{
    OFF,
    MSAA_2X,
    MSAA_4X,
    MSAA_8X
}

/**
 * Scaling technology enum
 */
public enum SCALING_TECH : byte
{
    BILINEAR,
    FSR_1,
    FSR_2
}

/**
 * Holds application settings values.
 */
public class ApplicationSettings
{
    public bool shadowsEnabled { get; set; } = true;
    public bool globalIlluminationEnabled { get; set; } = true;
    public bool fogEnabled { get; set; } = true;
    public bool ssaoEnabled { get; set; } = true;
    public bool ssilEnabled { get; set; } = true;
    public MSAA_SETTINGS msaaSettings { get; set; } = MSAA_SETTINGS.OFF;
    public SCALING_TECH scalingTech { get; set; } = SCALING_TECH.FSR_2;
    public float renderScale { get; set; } = 0.75f;
    public float fsrSharpness { get; set; } = 0.25f;
    public bool vsyncEnabled { get; set; } = true;
    public int maxFPS { get; set; } = 60;
    public int viewDistanceLod0 { get; set; } = 5;
    public int viewDistanceLod1 { get; set; } = 10;
    public int viewDistanceLod2 { get; set; } = 15;

    /**
     * Copies current values into a new instance
     */
    public ApplicationSettings Duplicate()
    {
        ApplicationSettings copy = new ApplicationSettings();
        copy.shadowsEnabled = this.shadowsEnabled;
        copy.globalIlluminationEnabled = this.globalIlluminationEnabled;
        copy.fogEnabled = this.fogEnabled;
        copy.ssaoEnabled = this.ssaoEnabled;
        copy.ssilEnabled = this.ssilEnabled;
        copy.msaaSettings = this.msaaSettings;
        copy.scalingTech = this.scalingTech;
        copy.renderScale = this.renderScale;
        copy.fsrSharpness = this.fsrSharpness;
        copy.vsyncEnabled = this.vsyncEnabled;
        copy.maxFPS = this.maxFPS;
        copy.viewDistanceLod0 = this.viewDistanceLod0;
        copy.viewDistanceLod1 = this.viewDistanceLod1;
        copy.viewDistanceLod2 = this.viewDistanceLod2;
        
        return copy;
    }
}

/**
 * Handles Settings GUI. Communicates with root of the project
 * and uses storage service for drive IO operations.
 */
public partial class Settings : CenterContainer
{
    private WorldEnvironment worldEnvironment;
    private DirectionalLight3D sunLight;
    private OmniLight3D playerTorchLight;
    private StorageService storageService;
    private Menu menu;

    private ApplicationSettings currentSettings = new();
    private ApplicationSettings changedSettings = new();
    private bool settingsChanged;
   
    private readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true
    };

    private const float renderScaleMin = 0.25f;
    private const float renderScaleMax = 2.0f;
    private const float fsrSharpnessMin = 0.0f;
    private const float fsrSharpnessMax = 2.0f;
    private const int maxFPSMin = 0;
    private const int maxFPSMax = 1000;
    private const int viewDistanceLodMin = 1;
    private const int viewDistanceLodMax = 256;
    
    // configuration of visibility ranges is disabled in world
    private bool visibilityChangesEnabled = true;
    
    // ui elements
    private HSlider lod0Slider;
    private SpinBox lod0SpinBox;
    private HSlider lod1Slider;
    private SpinBox lod1SpinBox;
    private HSlider lod2Slider;
    private SpinBox lod2SpinBox;
    
    private Button shadowsOffButton;
    private Button shadowsOnButton;

    private Button giOffButton;
    private Button giOnButton;
    
    private Button fogOffButton;
    private Button fogOnButton;
    
    private Button ssaoOffButton;
    private Button ssaoOnButton;
    
    private Button ssilOffButton;
    private Button ssilOnButton;
    
    private Button msaaOffButton;
    private Button msaa2xButton;
    private Button msaa4xButton;
    private Button msaa8xButton;
    
    private Button scalingBilinearButton;
    private Button scalingFSR1Button;
    private Button scalingFSR2Button;

    private HSlider scaleSlider;
    private SpinBox scaleSpinBox;
    
    private HSlider fsrSharpnessSlider;
    private SpinBox fsrSharpnessSpinBox;
    
    private Button vsyncOffButton;
    private Button vsyncOnButton;
    
    private HSlider maxFPSSlider;
    private SpinBox maxFPSSpinBox;

    private VBoxContainer visibilityRangesOffContainer;
    private VBoxContainer visibilityRangesOnContainer;

    public override void _Ready()
    {
        PreloadUINodes();
        ConfigureSettingsLimits();
    }

    /**
     * Configures necessary dependencies of Settings.
     */
    public void Setup(WorldEnvironment worldEnvironment, DirectionalLight3D sunLight, OmniLight3D playerTorchLight, StorageService storageService, Menu menu)
    {
        this.worldEnvironment = worldEnvironment;
        this.sunLight = sunLight;
        this.playerTorchLight = playerTorchLight;
        this.storageService = storageService;
        this.menu = menu;
        this.currentSettings = new ApplicationSettings();
    }

    /**
     * Obtain all ui node objects from the engine.
     */
    private void PreloadUINodes()
    {
        lod0Slider = GetNode<HSlider>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod0/Lod0Slider");
        lod0SpinBox = GetNode<SpinBox>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod0/Lod0SpinBox");
        lod1Slider = GetNode<HSlider>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod1/Lod1Slider");
        lod1SpinBox = GetNode<SpinBox>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod1/Lod1SpinBox");
        lod2Slider = GetNode<HSlider>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod2/Lod2Slider");
        lod2SpinBox = GetNode<SpinBox>("PanelContainer/Main/VisibilityRanges/VisibilityRangeLod2/Lod2SpinBox");
        
        shadowsOffButton = GetNode<Button>("PanelContainer/Main/Shadows/HBoxContainer/ShadowsOffButton");
        shadowsOnButton = GetNode<Button>("PanelContainer/Main/Shadows/HBoxContainer/ShadowsOnButton");
        
        giOffButton = GetNode<Button>("PanelContainer/Main/GlobalIllumination/HBoxContainer/GIOffButton");
        giOnButton = GetNode<Button>("PanelContainer/Main/GlobalIllumination/HBoxContainer/GIOnButton");
        
        fogOffButton = GetNode<Button>("PanelContainer/Main/Fog/HBoxContainer/FogOffButton");
        fogOnButton = GetNode<Button>("PanelContainer/Main/Fog/HBoxContainer/FogOnButton");
        
        ssaoOffButton = GetNode<Button>("PanelContainer/Main/SSAO/HBoxContainer/SSAOOffButton");
        ssaoOnButton = GetNode<Button>("PanelContainer/Main/SSAO/HBoxContainer/SSAOOnButton");
        
        ssilOffButton = GetNode<Button>("PanelContainer/Main/SSIL/HBoxContainer/SSILOffButton");
        ssilOnButton = GetNode<Button>("PanelContainer/Main/SSIL/HBoxContainer/SSILOnButton");
        
        msaaOffButton = GetNode<Button>("PanelContainer/Main/MSAA/HBoxContainer/MSAAOffButton");
        msaa2xButton = GetNode<Button>("PanelContainer/Main/MSAA/HBoxContainer/MSAA2XButton");
        msaa4xButton = GetNode<Button>("PanelContainer/Main/MSAA/HBoxContainer/MSAA4XButton");
        msaa8xButton = GetNode<Button>("PanelContainer/Main/MSAA/HBoxContainer/MSAA8XButton");
        
        scalingBilinearButton = GetNode<Button>("PanelContainer/Main/ScalingOptions/Technology/HBoxContainer/BilinearButton");
        scalingFSR1Button = GetNode<Button>("PanelContainer/Main/ScalingOptions/Technology/HBoxContainer/FSR1Button");
        scalingFSR2Button = GetNode<Button>("PanelContainer/Main/ScalingOptions/Technology/HBoxContainer/FSR2Button");
        
        scaleSlider = GetNode<HSlider>("PanelContainer/Main/ScalingOptions/Scale/ScaleSlider");
        scaleSpinBox = GetNode<SpinBox>("PanelContainer/Main/ScalingOptions/Scale/ScaleSpinBox");
        
        fsrSharpnessSlider = GetNode<HSlider>("PanelContainer/Main/ScalingOptions/FSRSharpness/SharpnessSlider");
        fsrSharpnessSpinBox = GetNode<SpinBox>("PanelContainer/Main/ScalingOptions/FSRSharpness/SharpnessSpinBox");
        
        vsyncOffButton = GetNode<Button>("PanelContainer/Main/VSync/HBoxContainer/VSyncOffButton");
        vsyncOnButton = GetNode<Button>("PanelContainer/Main/VSync/HBoxContainer/VSyncOnButton");
        
        maxFPSSlider = GetNode<HSlider>("PanelContainer/Main/MaxFPS/MaxFPSSlider");
        maxFPSSpinBox = GetNode<SpinBox>("PanelContainer/Main/MaxFPS/MaxFPSSpinBox");
        
        visibilityRangesOffContainer = GetNode<VBoxContainer>("PanelContainer/Main/VisibilityRangesOff");
        visibilityRangesOnContainer = GetNode<VBoxContainer>("PanelContainer/Main/VisibilityRanges");
    }
    
    /**
     * Toggles the ability of changing visibility ranges from the settings menu.
     */
    public void SetVisibilityRangesEnabled(bool enabled)
    {
        visibilityRangesOffContainer.Visible = !enabled;
        visibilityRangesOnContainer.Visible = enabled;
        visibilityChangesEnabled = enabled;
    }

    /**
     * Sets up GUI input values limits.
     */
    private void ConfigureSettingsLimits()
    {
        lod0Slider.MinValue = viewDistanceLodMin;
        lod0Slider.MaxValue = viewDistanceLodMax;
        lod0SpinBox.MinValue = viewDistanceLodMin;
        lod0SpinBox.MaxValue = viewDistanceLodMax;
        
        lod1Slider.MinValue = viewDistanceLodMin;
        lod1Slider.MaxValue = viewDistanceLodMax;
        lod1SpinBox.MinValue = viewDistanceLodMin;
        lod1SpinBox.MaxValue = viewDistanceLodMax;
        
        lod2Slider.MinValue = viewDistanceLodMin;
        lod2Slider.MaxValue = viewDistanceLodMax;
        lod2SpinBox.MinValue = viewDistanceLodMin;
        lod2SpinBox.MaxValue = viewDistanceLodMax;
        
        scaleSlider.MinValue = renderScaleMin;
        scaleSlider.MaxValue = renderScaleMax;
        scaleSpinBox.MinValue = renderScaleMin;
        scaleSpinBox.MaxValue = renderScaleMax;
        
        fsrSharpnessSlider.MinValue = fsrSharpnessMin;
        fsrSharpnessSlider.MaxValue = fsrSharpnessMax;
        fsrSharpnessSpinBox.MinValue = fsrSharpnessMin;
        fsrSharpnessSpinBox.MaxValue = fsrSharpnessMax;
        
        maxFPSSlider.MinValue = maxFPSMin;
        maxFPSSlider.MaxValue = maxFPSMax;
        maxFPSSpinBox.MinValue = maxFPSMin;
        maxFPSSpinBox.MaxValue = maxFPSMax;
    }
    
    /**
     * Loads all settings values from saved config file.
     * Distributes them throughout the settings menu and applies them
     * throughout the project. If settings can not be loaded, default is used instead.
     */
    public bool LoadApplySettings()
    {
        // apply default if storage service can not be used
        if (storageService == null)
        {
            currentSettings = new ApplicationSettings();
            changedSettings = currentSettings.Duplicate();
            DistributeAll(currentSettings);
            ApplyAllSettings();
            settingsChanged = false;
            return true;
        }
        
        String configText = storageService.LoadApplicationConfig();
        // fail or no config file, use defaults
        if (configText == null)
        {
            currentSettings = new ApplicationSettings();
            changedSettings = currentSettings.Duplicate();
            DistributeAll(currentSettings);
            ApplyAllSettings();
            settingsChanged = false;
            return true;
        }
        
        // parse config text
        if (!ParseSettings(configText))
        {
            currentSettings = new ApplicationSettings();
            changedSettings = currentSettings.Duplicate();
            settingsChanged = false;
        }
        // distribute and apply
        DistributeAll(currentSettings);
        ApplyAllSettings();
        return true;
    }

    /**
     * Saves current settings to config file.
     */
    private bool SaveSettingsToFile()
    {
        if (storageService == null)
            return false;
        
        String configText = SerializeSettings();
        if (configText == null)
            return false;
        
        return storageService.SaveApplicationConfig(configText);
    }

    /**
     * Distributes settings values throughout the GUI nodes.
     */
    private void DistributeAll(ApplicationSettings newSettings)
    {
        DistributeViewDistanceLod0(newSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(newSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(newSettings.viewDistanceLod2);
        DistributeShadows(newSettings.shadowsEnabled);
        DistributeGI(newSettings.globalIlluminationEnabled);
        DistributeFog(newSettings.fogEnabled);
        DistributeSSAO(newSettings.ssaoEnabled);
        DistributeSSIL(newSettings.ssilEnabled);
        DistributeMSAA(newSettings.msaaSettings);
        DistributeScalingTech(newSettings.scalingTech);
        DistributeRenderScale(newSettings.renderScale);
        DistributeFSRSharpness(newSettings.fsrSharpness);
        DistributeVSync(newSettings.vsyncEnabled);
        DistributeMaxFPS(newSettings.maxFPS);
    }
    
    /**
     * Parse and validate loaded settings from config file
     */
    private bool ParseSettings(String configText)
    {
        try
        {
            ApplicationSettings newSettings = JsonSerializer.Deserialize<ApplicationSettings>(configText) ?? new ApplicationSettings();
            // validate settings values
            newSettings.renderScale = Math.Clamp(newSettings.renderScale, renderScaleMin, renderScaleMax);
            newSettings.fsrSharpness = Math.Clamp(newSettings.fsrSharpness, fsrSharpnessMin, fsrSharpnessMax);
            newSettings.maxFPS =  Math.Clamp(newSettings.maxFPS, maxFPSMin, maxFPSMax);
            ValidateFixViewDistanceLods(newSettings);

            currentSettings = newSettings;
            changedSettings = currentSettings.Duplicate();
            settingsChanged = false;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /**
     * Serialize current settings into returned string.
     */
    private String SerializeSettings()
    {
        string json = JsonSerializer.Serialize(currentSettings, options);
        return json;
    }
    
    /**
     * Applies current settings throughout the project.
     */
    private void ApplyAllSettings()
    {
        ApplyShadowSettings();
        ApplyGISettings();
        ApplyFogSettings();
        ApplySSAOSettings();
        ApplySSILSettings();
        ApplyMSAASettings();
        ApplyScalingTechSettings();
        ApplyRenderScaleSettings();
        ApplyFSRSharpnessSettings();
        ApplyVSyncSettings();
        ApplyMaxFPSSettings();
        ApplyViewDistanceLodSettings();
    }

    /**
     * Applies shadows settings to all lights.
     */
    private void ApplyShadowSettings()
    {
        sunLight.ShadowEnabled = currentSettings.shadowsEnabled;
        playerTorchLight.ShadowEnabled = currentSettings.shadowsEnabled;
    }

    /**
     * Applies GI settings to world environment.
     */
    private void ApplyGISettings()
    {
        worldEnvironment.GetEnvironment().SdfgiEnabled = currentSettings.globalIlluminationEnabled;
    }
    
    /**
     * Applies fog settings to world environment.
     */
    private void ApplyFogSettings()
    {
        worldEnvironment.GetEnvironment().FogEnabled = currentSettings.fogEnabled;
    }

    /**
     * Applies SSAO settings to world environment.
     */
    private void ApplySSAOSettings()
    {
        worldEnvironment.GetEnvironment().SsaoEnabled = currentSettings.ssaoEnabled;
    }

    /**
     * Applies SSIL settings to world environment.
     */
    private void ApplySSILSettings()
    {
        worldEnvironment.GetEnvironment().SsilEnabled = currentSettings.ssilEnabled;
    }

    /**
     * Applies MSAA settings at project level.
     */
    private void ApplyMSAASettings()
    {
        Viewport viewport = GetViewport();
        switch (currentSettings.msaaSettings)
        {
            case MSAA_SETTINGS.OFF:
                viewport.SetMsaa3D(Viewport.Msaa.Disabled);
                break;
            case MSAA_SETTINGS.MSAA_2X:
                viewport.SetMsaa3D(Viewport.Msaa.Msaa2X);
                break;
            case MSAA_SETTINGS.MSAA_4X:
                viewport.SetMsaa3D(Viewport.Msaa.Msaa4X);
                break;
            case MSAA_SETTINGS.MSAA_8X:
                viewport.SetMsaa3D(Viewport.Msaa.Msaa8X);
                break;
        }
    }
    
    /**
     * Applies scaling technology choice at project level.
     */
    private void ApplyScalingTechSettings()
    {
        Viewport viewport = GetViewport();
        switch (currentSettings.scalingTech)
        {
            case SCALING_TECH.BILINEAR:
                viewport.SetScaling3DMode(Viewport.Scaling3DModeEnum.Bilinear);
                break;
            case SCALING_TECH.FSR_1:
                viewport.SetScaling3DMode(Viewport.Scaling3DModeEnum.Fsr);
                break;
            case SCALING_TECH.FSR_2:
                viewport.SetScaling3DMode(Viewport.Scaling3DModeEnum.Fsr2);
                break;
        }
    }
    
    /**
     * Applies render scale at project level.
     */
    private void ApplyRenderScaleSettings()
    {
        Viewport viewport = GetViewport();
        viewport.SetScaling3DScale(currentSettings.renderScale);
    }
    
    /**
     * Applies strength of FSR sharpening at project level.
     */
    private void ApplyFSRSharpnessSettings()
    {
        Viewport viewport = GetViewport();
        viewport.SetFsrSharpness(currentSettings.fsrSharpness);
    }
    
    /**
     * Applies VSync settings at project level.
     */
    private void ApplyVSyncSettings()
    {
        switch (currentSettings.vsyncEnabled)
        {
            case true:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
                break;
            case false:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                break;
        }
    }
    
    /**
     * Applies max FPS limit at project level.
     */
    private void ApplyMaxFPSSettings()
    {
        Engine.SetMaxFps(currentSettings.maxFPS);
    }

    /**
     * Signals new LOD distances to menu. Menu passes it to the root of the project.
     * Allowed only when in the title menu. Controlled from menu by visibilityChangesEnabled.
     */
    private void ApplyViewDistanceLodSettings()
    {
        // signal lod configuration to menu
        if (visibilityChangesEnabled)
        {
            menu.OnSetLodDistanceConfiguration(currentSettings.viewDistanceLod0, currentSettings.viewDistanceLod1, currentSettings.viewDistanceLod2);
        }
    }
    
    /**
     * Clamps view distance lod level values within limits.
     */
    private void ValidateFixViewDistanceLods(ApplicationSettings settings)
    {
        settings.viewDistanceLod0 = Math.Clamp(settings.viewDistanceLod0, viewDistanceLodMin, viewDistanceLodMax);
        settings.viewDistanceLod1 = Math.Clamp(settings.viewDistanceLod1, viewDistanceLodMin, viewDistanceLodMax);
        settings.viewDistanceLod2 = Math.Clamp(settings.viewDistanceLod2, viewDistanceLodMin, viewDistanceLodMax);

        settings.viewDistanceLod0 = Math.Clamp(settings.viewDistanceLod0, viewDistanceLodMin, settings.viewDistanceLod1);
        settings.viewDistanceLod1 = Math.Clamp(settings.viewDistanceLod1, settings.viewDistanceLod0, settings.viewDistanceLod2);
        settings.viewDistanceLod2 = Math.Clamp(settings.viewDistanceLod2, settings.viewDistanceLod1, viewDistanceLodMax);
    }
    
    // BUTTON HANDLERS
    
    private void _on_lod_0_slider_value_changed(float value)
    {
        changedSettings.viewDistanceLod0 = (int)value;
        DistributeViewDistanceLod0((int)value);
        settingsChanged = true;
    }

    private void _on_lod_0_spin_box_value_changed(float value)
    {
        changedSettings.viewDistanceLod0 = (int)value;
        DistributeViewDistanceLod0((int)value);
        settingsChanged = true;
    }

    private void DistributeViewDistanceLod0(int value)
    {
        lod0Slider.SetValueNoSignal(value);
        lod0SpinBox.SetValueNoSignal(value);
    }

    private void _on_lod_1_slider_value_changed(float value)
    {
        changedSettings.viewDistanceLod1 = (int)value;
        DistributeViewDistanceLod1((int)value);
        settingsChanged = true;
    }

    private void _on_lod_1_spin_box_value_changed(float value)
    {
        changedSettings.viewDistanceLod1 = (int)value;
        DistributeViewDistanceLod1((int)value);
        settingsChanged = true;
    }
    
    private void DistributeViewDistanceLod1(int value)
    {
        lod1Slider.SetValueNoSignal(value);
        lod1SpinBox.SetValueNoSignal(value);
    }

    private void _on_lod_2_slider_value_changed(float value)
    {
        changedSettings.viewDistanceLod2 = (int)value;
        DistributeViewDistanceLod2((int)value);
        settingsChanged = true;
    }

    private void _on_lod_2_spin_box_value_changed(float value)
    {
        changedSettings.viewDistanceLod2 = (int)value;
        DistributeViewDistanceLod2((int)value);
        settingsChanged = true;
    }
    
    private void DistributeViewDistanceLod2(int value)
    {
        lod2Slider.SetValueNoSignal(value);
        lod2SpinBox.SetValueNoSignal(value);
    }

    private void _on_shadows_off_button_button_up()
    {
        changedSettings.shadowsEnabled = false;
        settingsChanged = true;
        DistributeShadows(changedSettings.shadowsEnabled);
    }

    private void _on_shadows_on_button_button_up()
    {
        changedSettings.shadowsEnabled = true;
        settingsChanged = true;
        DistributeShadows(changedSettings.shadowsEnabled);
    }

    private void DistributeShadows(bool enabled)
    {
        shadowsOffButton.SetPressed(!enabled);
        shadowsOnButton.SetPressed(enabled);
    }

    private void _on_gi_off_button_button_up()
    {
        changedSettings.globalIlluminationEnabled = false;
        settingsChanged = true;
        DistributeGI(changedSettings.globalIlluminationEnabled);
    }

    private void _on_gi_on_button_button_up()
    {
        changedSettings.globalIlluminationEnabled = true;
        settingsChanged = true;
        DistributeGI(changedSettings.globalIlluminationEnabled);
    }
    
    private void DistributeGI(bool enabled)
    {
        giOffButton.SetPressed(!enabled);
        giOnButton.SetPressed(enabled);
    }

    private void _on_fog_off_button_button_up()
    {
        changedSettings.fogEnabled = false;
        settingsChanged = true;
        DistributeFog(changedSettings.fogEnabled);
    }

    private void _on_fog_on_button_button_up()
    {
        changedSettings.fogEnabled = true;
        settingsChanged = true;
        DistributeFog(changedSettings.fogEnabled);
    }
    
    private void DistributeFog(bool enabled)
    {
        fogOffButton.SetPressed(!enabled);
        fogOnButton.SetPressed(enabled);
    }

    private void _on_ssao_off_button_button_up()
    {
        changedSettings.ssaoEnabled = false;
        settingsChanged = true;
        DistributeSSAO(changedSettings.ssaoEnabled);
    }
    
    private void _on_ssao_on_button_button_up()
    {
        changedSettings.ssaoEnabled = true;
        settingsChanged = true;
        DistributeSSAO(changedSettings.ssaoEnabled);
    }
    
    private void DistributeSSAO(bool enabled)
    {
        ssaoOffButton.SetPressed(!enabled);
        ssaoOnButton.SetPressed(enabled);
    }

    private void _on_ssil_off_button_button_up()
    {
        changedSettings.ssilEnabled = false;
        settingsChanged = true;
        DistributeSSIL(changedSettings.ssilEnabled);
    }
    
    private void _on_ssil_on_button_button_up()
    {
        changedSettings.ssilEnabled = true;
        settingsChanged = true;
        DistributeSSIL(changedSettings.ssilEnabled);
    }
    
    private void DistributeSSIL(bool enabled)
    {
        ssilOffButton.SetPressed(!enabled);
        ssilOnButton.SetPressed(enabled);
    }

    private void _on_msaa_off_button_button_up()
    {
        changedSettings.msaaSettings = MSAA_SETTINGS.OFF;
        settingsChanged = true;
        DistributeMSAA(changedSettings.msaaSettings);
    }
    
    private void _on_msaa_2x_button_button_up()
    {
        changedSettings.msaaSettings = MSAA_SETTINGS.MSAA_2X;
        settingsChanged = true;
        DistributeMSAA(changedSettings.msaaSettings);
    }
    
    private void _on_msaa_4x_button_button_up()
    {
        changedSettings.msaaSettings = MSAA_SETTINGS.MSAA_4X;
        settingsChanged = true;
        DistributeMSAA(changedSettings.msaaSettings);
    }
    
    private void _on_msaa_8x_button_button_up()
    {
        changedSettings.msaaSettings = MSAA_SETTINGS.MSAA_8X;
        settingsChanged = true;
        DistributeMSAA(changedSettings.msaaSettings);
    }
    
    private void DistributeMSAA(MSAA_SETTINGS settings)
    {
        msaaOffButton.SetPressed(settings == MSAA_SETTINGS.OFF);
        msaa2xButton.SetPressed(settings == MSAA_SETTINGS.MSAA_2X);
        msaa4xButton.SetPressed(settings == MSAA_SETTINGS.MSAA_4X);
        msaa8xButton.SetPressed(settings == MSAA_SETTINGS.MSAA_8X);
    }

    private void _on_bilinear_button_button_up()
    {
        changedSettings.scalingTech = SCALING_TECH.BILINEAR;
        settingsChanged = true;
        DistributeScalingTech(changedSettings.scalingTech);
    }

    private void _on_fsr_1_button_button_up()
    {
        changedSettings.scalingTech = SCALING_TECH.FSR_1;
        settingsChanged = true;
        DistributeScalingTech(changedSettings.scalingTech);
    }

    private void _on_fsr_2_button_button_up()
    {
        changedSettings.scalingTech = SCALING_TECH.FSR_2;
        settingsChanged = true;
        DistributeScalingTech(changedSettings.scalingTech);
    }
    
    private void DistributeScalingTech(SCALING_TECH tech)
    {
        scalingBilinearButton.SetPressed(tech == SCALING_TECH.BILINEAR);
        scalingFSR1Button.SetPressed(tech == SCALING_TECH.FSR_1);
        scalingFSR2Button.SetPressed(tech == SCALING_TECH.FSR_2);
    }

    private void _on_scale_slider_value_changed(float value)
    {
        changedSettings.renderScale = value;
        settingsChanged = true;
        DistributeRenderScale(value);
    }

    private void _on_scale_spin_box_value_changed(float value)
    {
        changedSettings.renderScale = value;
        settingsChanged = true;
        DistributeRenderScale(value);
    }
    
    private void DistributeRenderScale(float value)
    {
        scaleSlider.SetValueNoSignal(value);
        scaleSpinBox.SetValueNoSignal(value);
    }

    private void _on_sharpness_slider_value_changed(float value)
    {
        changedSettings.fsrSharpness = value;
        settingsChanged = true;
        DistributeFSRSharpness(value);
    }

    private void _on_sharpness_spin_box_value_changed(float value)
    {
        changedSettings.fsrSharpness = value;
        settingsChanged = true;
        DistributeFSRSharpness(value);
    }
    
    private void DistributeFSRSharpness(float value)
    {
        fsrSharpnessSlider.SetValueNoSignal(value);
        fsrSharpnessSpinBox.SetValueNoSignal(value);
    }

    private void _on_v_sync_off_button_button_up()
    {
        changedSettings.vsyncEnabled = false;
        settingsChanged = true;
        DistributeVSync(changedSettings.vsyncEnabled);
    }
    
    private void _on_v_sync_on_button_button_up()
    {
        changedSettings.vsyncEnabled = true;
        settingsChanged = true;
        DistributeVSync(changedSettings.vsyncEnabled);
    }
    
    private void DistributeVSync(bool enabled)
    {
        vsyncOffButton.SetPressed(!enabled);
        vsyncOnButton.SetPressed(enabled);
    }
    
    private void _on_max_fps_slider_value_changed(float value)
    {
        changedSettings.maxFPS = (int)value;
        settingsChanged = true;
        DistributeMaxFPS((int)value);
    }
    
    private void _on_max_fps_spin_box_value_changed(float value)
    {
        changedSettings.maxFPS = (int)value;
        settingsChanged = true;
        DistributeMaxFPS((int)value);
    }
    
    private void DistributeMaxFPS(int value)
    {
        maxFPSSlider.SetValueNoSignal(value);
        maxFPSSpinBox.SetValueNoSignal(value);
    }

    /**
     * Navigates to previous menu through the menu script.
     */
    private void _on_back_button_up()
    {
        menu.OnSettingsBackButtonPressed();
    }
    
    // BUTTON HANDLING LOD PRESETS

    private void _on_very_low_button_up()
    {
        changedSettings.viewDistanceLod0 = 5;
        changedSettings.viewDistanceLod1 = 10;
        changedSettings.viewDistanceLod2 = 15;
        settingsChanged = true;
        DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
    }
    
    private void _on_low_button_up()
    {
        changedSettings.viewDistanceLod0 = 10;
        changedSettings.viewDistanceLod1 = 15;
        changedSettings.viewDistanceLod2 = 30;
        settingsChanged = true;
        DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
    }
    
    private void _on_medium_button_up()
    {
        changedSettings.viewDistanceLod0 = 15;
        changedSettings.viewDistanceLod1 = 25;
        changedSettings.viewDistanceLod2 = 45;
        settingsChanged = true;
        DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
    }
    
    private void _on_high_button_up()
    {
        changedSettings.viewDistanceLod0 = 20;
        changedSettings.viewDistanceLod1 = 40;
        changedSettings.viewDistanceLod2 = 80;
        settingsChanged = true;
        DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
    }
    
    private void _on_ultra_button_up()
    {
        changedSettings.viewDistanceLod0 = 30;
        changedSettings.viewDistanceLod1 = 50;
        changedSettings.viewDistanceLod2 = 120;
        settingsChanged = true;
        DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
        DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
        DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
    }
    
    /**
     * Processes values, distributes them and applies within the project.
     */
    private void _on_apply_button_up()
    {
        if (!settingsChanged)
            return;
        if (visibilityChangesEnabled)
        {
            ValidateFixViewDistanceLods(changedSettings);
            DistributeViewDistanceLod0(changedSettings.viewDistanceLod0);
            DistributeViewDistanceLod1(changedSettings.viewDistanceLod1);
            DistributeViewDistanceLod2(changedSettings.viewDistanceLod2);
        }
        
        currentSettings = changedSettings.Duplicate();
        ApplyAllSettings();
        SaveSettingsToFile();
        settingsChanged = false;
    }
}
