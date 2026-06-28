// FILE: Menu.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the complete GUI Menu (except Settings)

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Handles control of the in game Menu.
 * Switches between menus, controls other parts of the project based on user input.
 * Includes title menu, world selection, world creation and in-game pause menu. Also uses Settings menu component.
 */
public partial class Menu : Control
{
    private Root root;
    private Player player;
    private StorageService storageService;

    // ui elements
    private TextureRect background;
    private Label centeredMessage;
    private Node2D crosshair;
    private Settings settings;
    private PanelContainer titleMenu;
    private PanelContainer worldsSelection;
    private PanelContainer newWorld;
    private HBoxContainer pauseMenu;
    private PanelContainer alertDialog;
    private Label alertDialogLabel;

    private LineEdit worldNameEdit;
    private SpinBox worldCreationSeedSpinBox;
    private HSlider worldCreationChunkCountYSlider;

    private OptionButton worldCreationSettingsProfileSelect;
    private OptionButton worldCreationTerrainConfigSelect;
    private OptionButton worldCreationVegetationDensitySelect;
    private OptionButton worldCreationLakeSizeSelect;
    private OptionButton worldCreationWaterAvailabilityStrengthSelect;
    
    private HSlider worldCreationAreaAgeVeryYoungSlider;
    private HSlider worldCreationAreaAgeYoungSlider;
    private HSlider worldCreationAreaAgeYoungMediumSlider;
    private HSlider worldCreationAreaAgeMediumSlider;
    private HSlider worldCreationAreaAgeMediumOldSlider;
    private HSlider worldCreationAreaAgeOldSlider;
    private HSlider worldCreationAreaAgeVeryOldSlider;
    private HSlider worldCreationAreaAgeAncientSlider;
    private HSlider worldCreationAreaHeightLowlandSlider;
    private HSlider worldCreationAreaHeightHillsSlider;
    private HSlider worldCreationAreaHeightMountainsSlider;
    private HSlider worldCreationAreaWaterAvailabilityHighSlider;
    private HSlider worldCreationAreaWaterAvailabilityMediumSlider;
    private HSlider worldCreationAreaWaterAvailabilityLowSlider;
    private HSlider worldCreationAreaWaterAvailabilityAridSlider;

    private HSlider worldCreationWaterLilyChanceSlider;
    private HSlider worldCreationWaterReedsChanceSlider;
    private HSlider worldCreationSnowChanceSlider;
    
    private CheckButton worldCreationCavesEnabledButton;
    private HSlider worldCreationCavesStartOffsetSlider;
    private HSlider worldCreationCavesMaxWaterLevelSlider;
    private HSlider worldCreationCavesSpaceRatioSlider;
    private HSlider worldCreationCavesTunnelsRatioSlider;
    private HSlider worldCreationCavesTunnelEntrancesRatioSlider;
    private HSlider worldCreationCavesStalactitesChanceSlider;
    private HSlider worldCreationCavesWaterfallChanceSlider;

    private HSlider worldCreationWaterPrefillTerrainHeightSlider;
    private HSlider worldCreationMaxRiverLengthSlider;
    private HSlider worldCreationExplicitWaterAvailabilityEffectSlider;

    private SpinBox teleportXInput;
    private SpinBox teleportZInput;

    private VBoxContainer worldsContainer;
    
    private GridContainer voxelEditTypeItemsContainer;
    private VoxelTypeItem[] voxelEditTypeItems;
    
    // current world configuration
    private WorldSaveConfig currentWorldSaveConfig;
    // preloaded scenes of dynamic GUI components
    private static readonly PackedScene _voxelTypeItemScene = GD.Load<PackedScene>("res://Scenes/VoxelTypeItem.tscn");
    private static readonly PackedScene _worldNameItemScene = GD.Load<PackedScene>("res://Scenes/WorldSelectItem.tscn");

    private int defaultPresetIndex; // index of default world configuration preset
    private WorldSettingsPreset[] worldSettingsPresets;
    
    // menu / game states
    private bool inGame;
    private bool menuOpen;
    private bool mouseModeCaptured;
    // after how many frames is editing re-enabled, to stop bleeding mouse clicks to voxel editing when pause menu is closed
    private int framesToReenableEditing = -1; // -1 nothing, 0 switch now, > 0 count down frames
    private bool alreadySavedOnExit;

    // world limits in meters from the center
    private int worldLimitMetersXMin;
    private int worldLimitMetersXMax;
    private int worldLimitMetersZMin;
    private int worldLimitMetersZMax;

    public override void _Ready()
    {
        InitializeWorldPresets();
        InitializeUINodes();
        FillVoxelEditTypeItems();
        InitializeNewWorldUI();
    }

    /**
     * Sets up the menu with dependencies and loads application settings from config file.
     */
    public void Setup(Root root, Player player, WorldEnvironment worldEnvironment, DirectionalLight3D sun, StorageService storageService, int worldLimitMetersXMin, int worldLimitMetersXMax, int worldLimitMetersZMin, int worldLimitMetersZMax, VoxelType voxelEditType, byte voxelEditSize)
    {
        this.root = root;
        this.player = player;
        this.storageService = storageService;
        this.worldLimitMetersXMin = worldLimitMetersXMin;
        this.worldLimitMetersXMax = worldLimitMetersXMax;
        this.worldLimitMetersZMin = worldLimitMetersZMin;
        this.worldLimitMetersZMax = worldLimitMetersZMax;
        
        teleportXInput.MinValue = worldLimitMetersXMin;
        teleportXInput.MaxValue = worldLimitMetersXMax;
        teleportZInput.MinValue = worldLimitMetersZMin;
        teleportZInput.MaxValue = worldLimitMetersZMax;
        
        settings.Setup(worldEnvironment, sun, player.GetTorch(), storageService, this);
        settings.LoadApplySettings();
        
        SpinBox voxelEditSizeSpinBox = GetNode<SpinBox>("Menu/PauseMenu/EditTeleportMenu/EditTeleportVBox/VoxelEditMenu/Options/VoxelSize/VoxelSizeEdit");
        voxelEditSizeSpinBox.Value = Math.Log2(voxelEditSize) + 1;
        OnVoxelEditTypeItemSelected((int)voxelEditType, false);
    }

    /**
     * Engine process function.
     * Handles voxel editing re-enabling if scheduled, and pause / in-game switching.
     */
    public override void _Process(double delta)
    {
        // decrease / signal editing re-enabled
        if (framesToReenableEditing != -1)
        {
            if (framesToReenableEditing == 0)
            {
                root.OnEditingStatusChange(true, true);
                framesToReenableEditing = -1;
            }
            else
            {
                framesToReenableEditing--;
            }
        }
        
        // switch to pause menu
        if (Input.IsActionJustPressed("pause_menu"))
        {
            if (inGame)
            {
                if (menuOpen)
                {
                    SwitchToInGame();
                }
                else
                {
                    SwitchToPauseMenu();
                }
            }
        }
    }

    /**
     * Initialize world presets.
     * TODO: might move this elsewhere
     */
    private void InitializeWorldPresets()
    {
        worldSettingsPresets = new WorldSettingsPreset[7];
        
        // default normal preset
        worldSettingsPresets[0] = new WorldSettingsPreset(
            "Default",
            new WorldSettings(),
            [15,15,12,8,11,14,13,12],
            [30, 25, 45],
            [6, 3, 5, 11],
            0.125f,
            0.1111f,
            0.6667f,
            0.25f
            );
        defaultPresetIndex = 0;
        // no trees
        worldSettingsPresets[1] = new WorldSettingsPreset(
            "Without Trees",
            new WorldSettings(),
            [5,5,0,0,0,0,0,0],
            [30, 25, 45],
            [6, 3, 5, 11],
            0.4583f,
            0.35f,
            0.5f,
            0.25f
        );
        // deep caves
        worldSettingsPresets[2] = new WorldSettingsPreset(
            "Deep Caves",
            new WorldSettings()
            {
                cavesStartTerrainHeightRatio = 0.05f,
                cavesStalactitesChance = 200,
            },
            [15,15,12,8,11,14,13,12],
            [30, 25, 45],
            [6, 3, 5, 11],
            1.0f,
            1.0f,
            0.75f,
            0.25f
        );
        // dry land
        worldSettingsPresets[3] = new WorldSettingsPreset(
            "Dry Land",
            new WorldSettings()
            {
                lakeSize = LakeSizeType.SMALL,
                explicitWaterLevelTerrainHeightRatio = 0.01f,
                waterAvailabilityStrength = WaterAvailEffectStrengthType.LOW,
                maxRiverLength = 10,
                explicitWaterAvailabilityMaxEffect = 80,
                waterReedsChance = 40,
                waterLilyChance = 4,
            },
            [15,15,12,8,11,14,13,12],
            [30, 25, 45],
            [20,15,10,5],
            0.4583f,
            0.35f,
            0.5f,
            0.25f
        );
        // snowy world
        worldSettingsPresets[4] = new WorldSettingsPreset(
            "Snowy World",
            new WorldSettings(),
            [15,15,12,8,11,14,13,12],
            [0, 0, 10],
            [6, 3, 5, 11],
            0.4583f,
            0.35f,
            0.5f,
            1.0f
        );
        // desert world
        worldSettingsPresets[5] = new WorldSettingsPreset(
            "Desert",
            new WorldSettings()
            {
                lakeSize = LakeSizeType.NONE,
                explicitWaterLevelTerrainHeightRatio = 0.01f,
                waterAvailabilityStrength = WaterAvailEffectStrengthType.LOW,
                maxRiverLength = 0,
                explicitWaterAvailabilityMaxEffect = 0,
                waterReedsChance = 0,
                waterLilyChance = 0,
            },
            [20,20,1,1,1,1,1,1],
            [10,0,0],
            [10, 0, 0, 0],
            0.4583f,
            0.35f,
            0.5f,
            0.25f
        );
        // lush vegetation
        worldSettingsPresets[6] = new WorldSettingsPreset(
            "Lush Vegetation",
            new WorldSettings()
            {
                overallVegetationChance = VegChanceType.VERY_HIGH,
                waterLilyChance = 100,
                waterReedsChance = 500,
                explicitWaterLevelTerrainHeightRatio = 0.1f,
                explicitWaterAvailabilityMaxEffect = 224,
            },
            [15,15,12,8,11,14,13,12],
            [30, 25, 45],
            [2, 4, 7, 15],
            0.125f,
            0.1111f,
            0.6667f,
            0.25f
        );

    }

    /**
     * Obtain all important ui node objects for access later.
     */
    private void InitializeUINodes()
    {
        background = GetNode<TextureRect>("Background");
        centeredMessage = GetNode<Label>("Menu/CenteredMessage");
        crosshair = GetNode<Node2D>("Menu/Crosshair");
        settings = GetNode<Settings>("Menu/Settings");
        titleMenu = GetNode<PanelContainer>("Menu/TitleMenu");
        worldsSelection = GetNode<PanelContainer>("Menu/WorldsSelection");
        newWorld = GetNode<PanelContainer>("Menu/NewWorld");
        pauseMenu = GetNode<HBoxContainer>("Menu/PauseMenu");
        alertDialog = GetNode<PanelContainer>("Menu/AlertDialog");
        alertDialogLabel = GetNode<Label>("Menu/AlertDialog/VBoxContainer/AlertMessageLabel");

        worldsContainer = GetNode<VBoxContainer>("Menu/WorldsSelection/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/Worlds");
        voxelEditTypeItemsContainer = GetNode<GridContainer>("Menu/PauseMenu/EditTeleportMenu/EditTeleportVBox/VoxelEditMenu/Options/VoxelType/VoxelTypeContainer");
        
        worldNameEdit = GetNode<LineEdit>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/WorldNameSetting/WorldNameEdit");
        worldCreationSeedSpinBox = GetNode<SpinBox>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/SeedSetting/SeedSpinBox");
        worldCreationChunkCountYSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/ChunkCountY/ChunkCountYSlider");
        
        worldCreationSettingsProfileSelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/WorldProfile/HBoxContainer/WorldProfileSelect");
        worldCreationTerrainConfigSelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/TerrainConfig/TerrainConfigSelect");
        worldCreationVegetationDensitySelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/VegetationDensity/VegetationDensitySelect");
        worldCreationLakeSizeSelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/WaterSettings/LakeSize/LakeSizeSelect");
        worldCreationWaterAvailabilityStrengthSelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/WaterSettings/WaterAvailabilityStrength/WaterAvailabilityStrengthSelect");
        worldCreationTerrainConfigSelect = GetNode<OptionButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/TerrainConfig/TerrainConfigSelect");
        
        worldCreationAreaAgeVeryYoungSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/VeryYoung/VeryYoungSlider");
        worldCreationAreaAgeYoungSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/Young/YoungSlider");
        worldCreationAreaAgeYoungMediumSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/YoungMedium/YoungMediumSlider");
        worldCreationAreaAgeMediumSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/Medium/MediumSlider");
        worldCreationAreaAgeMediumOldSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/MediumOld/MediumOldSlider");
        worldCreationAreaAgeOldSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/Old/OldSlider");
        worldCreationAreaAgeVeryOldSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/VeryOld/VeryOldSlider");
        worldCreationAreaAgeAncientSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaAgeSettings/AreaAgeThresholds/Ancient/AncientSlider");
        worldCreationAreaHeightLowlandSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaHeightSettings/AreaHeightThresholds/Lowland/LowlandSlider");
        worldCreationAreaHeightHillsSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaHeightSettings/AreaHeightThresholds/Hills/HillsSlider");
        worldCreationAreaHeightMountainsSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaHeightSettings/AreaHeightThresholds/Mountains/MountainsSlider");
        worldCreationAreaWaterAvailabilityHighSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaWaterAvailabilitySettings/AreaWaterAvailabilityThresholds/High/WaterHighSlider");
        worldCreationAreaWaterAvailabilityMediumSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaWaterAvailabilitySettings/AreaWaterAvailabilityThresholds/Medium/WaterMediumSlider");
        worldCreationAreaWaterAvailabilityLowSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaWaterAvailabilitySettings/AreaWaterAvailabilityThresholds/Low/WaterLowSlider");
        worldCreationAreaWaterAvailabilityAridSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/AreaWaterAvailabilitySettings/AreaWaterAvailabilityThresholds/Arid/WaterAridSlider");
        
        worldCreationWaterLilyChanceSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/WaterLilyChance/WaterLilySlider");
        worldCreationWaterReedsChanceSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/WaterReedsChance/WaterReedsChanceSlider");
        worldCreationSnowChanceSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/OverallSettings/SnowChance/SnowChanceSlider");
        
        worldCreationCavesEnabledButton = GetNode<CheckButton>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesToggle/CavesToggleButton");
        worldCreationCavesStartOffsetSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesStartHeightOffset/CavesStartOffsetSlider");
        worldCreationCavesMaxWaterLevelSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesMaxWaterLevel/CavesMaxWaterLevelSlider");
        worldCreationCavesSpaceRatioSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesSpaceRatio/CavesSpaceRationSlider");
        worldCreationCavesTunnelsRatioSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesTunnelsRatio/CavesTunnelsRatioSlider");
        worldCreationCavesTunnelEntrancesRatioSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesTunnelEntrancesRatio/CavesTunnelEntrancesRatioSlider");
        worldCreationCavesStalactitesChanceSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesStalactitesChance/CavesStalactitesChanceSlider");
        worldCreationCavesWaterfallChanceSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/CavesSettings/CavesWaterfallChance/CavesWaterfallChanceSlider");
        
        worldCreationWaterPrefillTerrainHeightSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/WaterSettings/WaterPrefillTerrainHeight/WaterPrefillTerrainHeightSlider");
        worldCreationMaxRiverLengthSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/WaterSettings/MaxRiverLength/MaxRiverLengthSlider");
        worldCreationExplicitWaterAvailabilityEffectSlider = GetNode<HSlider>("Menu/NewWorld/VBoxContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldSettings/WaterSettings/ImplicitWaterAvailabilityEffect/ImplicitWaterAvailabilityEffectSlider");
        
        teleportXInput = GetNode<SpinBox>("Menu/PauseMenu/EditTeleportMenu/EditTeleportVBox/TeleportMenu/PositionsContainer/TeleportPositionX/TeleportPositionXEdit"); 
        teleportZInput = GetNode<SpinBox>("Menu/PauseMenu/EditTeleportMenu/EditTeleportVBox/TeleportMenu/PositionsContainer/TeleportPositionZ/TeleportPositionZEdit");
    }

    /**
     * Initialize input ui in new world creation section with limit values.
     */
    private void InitializeNewWorldUILimits()
    {
        worldCreationSeedSpinBox.MinValue = WorldSettings.seedMin;
        worldCreationSeedSpinBox.MaxValue = WorldSettings.seedMax;
        worldCreationChunkCountYSlider.MinValue = WorldSettings.terrainChunkCountYMin;
        worldCreationChunkCountYSlider.MaxValue = WorldSettings.terrainChunkCountYMax;
        worldCreationAreaAgeVeryYoungSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeVeryYoungSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeYoungSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeYoungSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeYoungMediumSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeYoungMediumSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeMediumSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeMediumSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeMediumOldSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeMediumOldSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeOldSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeOldSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeVeryOldSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeVeryOldSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaAgeAncientSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaAgeAncientSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaHeightLowlandSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaHeightLowlandSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaHeightHillsSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaHeightHillsSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaHeightMountainsSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaHeightMountainsSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaWaterAvailabilityHighSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaWaterAvailabilityHighSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaWaterAvailabilityMediumSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaWaterAvailabilityMediumSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaWaterAvailabilityLowSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaWaterAvailabilityLowSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationAreaWaterAvailabilityAridSlider.MinValue = WorldSettings.distributionInputsMin;
        worldCreationAreaWaterAvailabilityAridSlider.MaxValue = WorldSettings.distributionInputsMax;
        worldCreationWaterLilyChanceSlider.MinValue = WorldSettings.waterLilyChanceMin;
        worldCreationWaterLilyChanceSlider.MaxValue = WorldSettings.waterLilyChanceMax;
        worldCreationWaterReedsChanceSlider.MinValue = WorldSettings.waterReedsChanceMin;
        worldCreationWaterReedsChanceSlider.MaxValue = WorldSettings.waterReedsChanceMax;
        worldCreationSnowChanceSlider.MinValue = WorldSettings.snowChanceMin;
        worldCreationSnowChanceSlider.MaxValue = WorldSettings.snowChanceMax;
        worldCreationCavesStartOffsetSlider.MinValue = WorldSettings.cavesStartTerrainHeightRatioMin;
        worldCreationCavesStartOffsetSlider.MaxValue = WorldSettings.cavesStartTerrainHeightRatioMax;
        worldCreationCavesMaxWaterLevelSlider.MinValue = WorldSettings.cavesWaterLevelTerrainHeightRatioMin;
        worldCreationCavesMaxWaterLevelSlider.MaxValue = WorldSettings.cavesWaterLevelTerrainHeightRatioMax;
        worldCreationCavesSpaceRatioSlider.MinValue = WorldSettings.ratioInputsMin;
        worldCreationCavesSpaceRatioSlider.MaxValue = WorldSettings.ratioInputsMax;
        worldCreationCavesTunnelsRatioSlider.MinValue = WorldSettings.ratioInputsMin;
        worldCreationCavesTunnelsRatioSlider.MaxValue = WorldSettings.ratioInputsMax;
        worldCreationCavesTunnelEntrancesRatioSlider.MinValue = WorldSettings.ratioInputsMin;
        worldCreationCavesTunnelEntrancesRatioSlider.MaxValue = WorldSettings.ratioInputsMax;
        worldCreationCavesStalactitesChanceSlider.MinValue = WorldSettings.cavesStalactitesChanceMin;
        worldCreationCavesStalactitesChanceSlider.MaxValue = WorldSettings.cavesStalactitesChanceMax;
        worldCreationCavesWaterfallChanceSlider.MinValue = WorldSettings.cavesWaterfallChanceMin;
        worldCreationCavesWaterfallChanceSlider.MaxValue = WorldSettings.cavesWaterfallChanceMax;
        worldCreationWaterPrefillTerrainHeightSlider.MinValue = WorldSettings.explicitWaterLevelTerrainHeightRatioMin;
        worldCreationWaterPrefillTerrainHeightSlider.MaxValue = WorldSettings.explicitWaterLevelTerrainHeightRatioMax;
        worldCreationMaxRiverLengthSlider.MinValue = WorldSettings.maxRiverLengthMin;
        worldCreationMaxRiverLengthSlider.MaxValue = WorldSettings.maxRiverLengthMax;
        worldCreationExplicitWaterAvailabilityEffectSlider.MinValue = WorldSettings.explicitWaterAvailabilityMaxEffectMin;
        worldCreationExplicitWaterAvailabilityEffectSlider.MaxValue = WorldSettings.explicitWaterAvailabilityMaxEffectMax;
    }

    /**
     * Initialize select input nodes to values of default preset
     */
    private void InitializeNewWorldUISelectBoxes()
    {
        worldCreationSettingsProfileSelect.Clear();
        for (int presetIndex = 0; presetIndex < worldSettingsPresets.Length; presetIndex++)
        {
            worldCreationSettingsProfileSelect.AddItem(worldSettingsPresets[presetIndex].name, presetIndex);
        }
        worldCreationSettingsProfileSelect.Selected = defaultPresetIndex;
        
        worldCreationTerrainConfigSelect.Clear();
        HeightNoiseConfigType[] terrainConfigTypes = (HeightNoiseConfigType[])Enum.GetValues(typeof(HeightNoiseConfigType));
        foreach (HeightNoiseConfigType type in terrainConfigTypes)
        {
            worldCreationTerrainConfigSelect.AddItem(HeightNoiseConfig.ToString(type), (int)type);
        }
        
        worldCreationVegetationDensitySelect.Clear();
        VegChanceType[] vegetationTypes = (VegChanceType[])Enum.GetValues(typeof(VegChanceType));
        foreach (VegChanceType type in vegetationTypes)
        {
            worldCreationVegetationDensitySelect.AddItem(VegetationChance.ToString(type), (int)type);
        }
        
        worldCreationLakeSizeSelect.Clear();
        LakeSizeType[] lakeSizeTypes = (LakeSizeType[])Enum.GetValues(typeof(LakeSizeType));
        foreach (LakeSizeType type in lakeSizeTypes)
        {
            worldCreationLakeSizeSelect.AddItem(LakeSize.ToString(type), (int)type);
        }
        
        worldCreationWaterAvailabilityStrengthSelect.Clear();
        WaterAvailEffectStrengthType[] waterAvailabilityStrengthTypes = (WaterAvailEffectStrengthType[])Enum.GetValues(typeof(WaterAvailEffectStrengthType));
        foreach (WaterAvailEffectStrengthType type in waterAvailabilityStrengthTypes)
        {
            worldCreationWaterAvailabilityStrengthSelect.AddItem(WaterAvailEffectStrength.ToString(type), (int)type);
        }
    }

    /**
     * Initializes UI of new world creation.
     */
    private void InitializeNewWorldUI()
    {
        currentWorldSaveConfig = new WorldSaveConfig();
        InitializeNewWorldUILimits();
        InitializeNewWorldUISelectBoxes();
        FillNewWorldUIPresetValues(defaultPresetIndex);

        worldCreationSeedSpinBox.Value = currentWorldSaveConfig.worldSettings.seed;
        worldCreationChunkCountYSlider.Value = currentWorldSaveConfig.worldSettings.terrainChunkCountY;
    }

    /**
     * Initializes new world creation ui input elements with values from default preset.
     */
    private void FillNewWorldUIPresetValues(int presetIndex)
    {
        WorldSettingsPreset preset = worldSettingsPresets[presetIndex];
        WorldSettings settings = preset.settings;
        
        worldCreationChunkCountYSlider.Value = settings.terrainChunkCountY;
        worldCreationTerrainConfigSelect.Selected = (int)settings.heightNoiseConfig;
        // age distribution
        worldCreationAreaAgeVeryYoungSlider.Value = preset.ageDistributionInputs[0];
        worldCreationAreaAgeYoungSlider.Value = preset.ageDistributionInputs[1];
        worldCreationAreaAgeYoungMediumSlider.Value = preset.ageDistributionInputs[2];
        worldCreationAreaAgeMediumSlider.Value = preset.ageDistributionInputs[3];
        worldCreationAreaAgeMediumOldSlider.Value = preset.ageDistributionInputs[4];
        worldCreationAreaAgeOldSlider.Value = preset.ageDistributionInputs[5];
        worldCreationAreaAgeVeryOldSlider.Value = preset.ageDistributionInputs[6];
        worldCreationAreaAgeAncientSlider.Value = preset.ageDistributionInputs[7];
        // height distribution
        worldCreationAreaHeightLowlandSlider.Value = preset.heightDistributionInputs[0];
        worldCreationAreaHeightHillsSlider.Value = preset.heightDistributionInputs[1];
        worldCreationAreaHeightMountainsSlider.Value = preset.heightDistributionInputs[2];
        // water availability distribution
        worldCreationAreaWaterAvailabilityAridSlider.Value = preset.waterAvailabilityDistributionInputs[0];
        worldCreationAreaWaterAvailabilityLowSlider.Value = preset.waterAvailabilityDistributionInputs[1];
        worldCreationAreaWaterAvailabilityMediumSlider.Value = preset.waterAvailabilityDistributionInputs[2];
        worldCreationAreaWaterAvailabilityHighSlider.Value = preset.waterAvailabilityDistributionInputs[3];
        
        worldCreationVegetationDensitySelect.Selected = (int)settings.overallVegetationChance;
        worldCreationWaterLilyChanceSlider.Value = settings.waterLilyChance;
        worldCreationWaterReedsChanceSlider.Value = settings.waterReedsChance;
        worldCreationSnowChanceSlider.Value = preset.snowThresholdRatioInput;
        worldCreationCavesEnabledButton.SetPressedNoSignal(settings.cavesEnabled);
        worldCreationCavesStartOffsetSlider.Value = settings.cavesStartTerrainHeightRatio;
        worldCreationCavesMaxWaterLevelSlider.Value = settings.cavesWaterLevelTerrainHeightRatio;
        worldCreationCavesSpaceRatioSlider.Value = preset.cavesSpaceRatioInput;
        worldCreationCavesTunnelsRatioSlider.Value = preset.cavesTunnelsChanceRatioInput;
        worldCreationCavesTunnelEntrancesRatioSlider.Value = preset.cavesTunnelEntrancesChanceRatioInput;
        worldCreationCavesStalactitesChanceSlider.Value = settings.cavesStalactitesChance;
        worldCreationCavesWaterfallChanceSlider.Value = settings.cavesWaterfallChance;
        worldCreationWaterPrefillTerrainHeightSlider.Value = settings.explicitWaterLevelTerrainHeightRatio;
        worldCreationLakeSizeSelect.Selected = (int)settings.lakeSize;
        worldCreationMaxRiverLengthSlider.Value = settings.maxRiverLength;
        worldCreationWaterAvailabilityStrengthSelect.Selected = (int)settings.waterAvailabilityStrength;
        worldCreationExplicitWaterAvailabilityEffectSlider.Value = settings.explicitWaterAvailabilityMaxEffect;
    }
    
    /**
     * Initializes voxel type selection items based on available voxel types.
     */
    private void FillVoxelEditTypeItems()
    {
        VoxelType[] voxelTypes = (VoxelType[])Enum.GetValues(typeof(VoxelType));
        voxelEditTypeItems = new VoxelTypeItem[voxelTypes.Length];
        for (int i = 0; i < voxelTypes.Length; i++)
        {
            VoxelType voxelType = voxelTypes[i];
            VoxelTypeItem item = _voxelTypeItemScene.Instantiate<VoxelTypeItem>();
            voxelEditTypeItemsContainer.AddChild(item);
            item.SetVoxelType(voxelType);
            item.WireUpOnClicked(OnVoxelEditTypeItemSelected, i);
            voxelEditTypeItems[i] = item;
        }
    }
    
    /**
     * Shows centered message. Diables everything else.
     */
    public void ShowCenteredMessageLoading(string message)
    {
        centeredMessage.Text = message;
        centeredMessage.Visible = true;
        background.Visible = true;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = false;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        inGame = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Hides centered message.
     */
    public void HideCenteredMessage()
    {
        centeredMessage.Visible = false;
    }

    /**
     * Switches menu to in-game settings. This menu does not allow editing of visibility ranges.
     */
    private void SwitchToSettingsInGame()
    {
        settings.SetVisibilityRangesEnabled(false);
        background.Visible = false;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = true;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Switches to fully featured seetings from title menu.
     */
    private void SwitchToSettingsInTitle()
    {
        settings.SetVisibilityRangesEnabled(true);
        background.Visible = true;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = true;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        inGame = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Switches ui to tile menu.
     */
    public void SwitchToTitleMenu()
    {
        background.Visible = true;
        titleMenu.Visible = true;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = false;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        inGame = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Switches ui to worlds selection.
     * Worlds are loaded through storage service.
     */
    private void SwitchToWorldsSelection()
    {
        // obtain list of available worlds
        List<WorldSaveConfig> worlds = storageService.ObtainSavedWorldsList();
        // clear previous
        Godot.Collections.Array<Godot.Node> worldItemsContainer = worldsContainer.GetChildren();
        foreach (Godot.Node worldItem in worldItemsContainer)
        {
            worldItem.QueueFree();
        }
        // display new
        foreach (WorldSaveConfig world in worlds)
        {
            WorldSelectItem item = _worldNameItemScene.Instantiate<WorldSelectItem>();
            worldsContainer.AddChild(item);
            item.SetWorldName(world.worldName);
            item.WireUpOnClickedOpen(OnWorldSelected, world);
            item.WireUpOnClickedDelete(OnWorldDelete, world.worldDirectoryName);
        }

        background.Visible = true;
        titleMenu.Visible = false;
        worldsSelection.Visible = true;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = false;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        inGame = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Switches to new world creation menu.
     */
    private void SwitchToNewWorld()
    {
        background.Visible = true;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = true;
        pauseMenu.Visible = false;
        settings.Visible = false;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        inGame = false;
        menuOpen = true;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }
    
    /**
     * Switches to pause menu.
     */
    private void SwitchToPauseMenu()
    {
        background.Visible = false;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = true;
        settings.Visible = false;
        crosshair.Visible = false;
        alertDialog.Visible = false;
        menuOpen = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        player.DisableControls();
        root.OnEditingStatusChange(false, false);
    }

    /**
     * Switches to game menu free view.
     * Re-enables player controls and schedules re-enable of voxel editing in next frame.
     */
    private void SwitchToInGame()
    {
        background.Visible = false;
        titleMenu.Visible = false;
        worldsSelection.Visible = false;
        newWorld.Visible = false;
        pauseMenu.Visible = false;
        settings.Visible = false;
        crosshair.Visible = true;
        alertDialog.Visible = false;
        inGame = true;
        menuOpen = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        framesToReenableEditing = 1;
        player.EnableControls();
    }

    /**
     * Processes player teleport request via root.
     * Position is already clamped in godot UI.
     */
    private void _on_teleport_button_button_up()
    {
        float posX = (float)teleportXInput.Value;
        float posZ = (float)teleportZInput.Value;
        Vector3Double realPosition = new Vector3Double(posX, player.GetRealPosition().y, posZ);
        
        root.TeleportPlayer(realPosition);
    }

    /**
     * Signals voxel editing size change to root.
     */
    private void _on_voxel_size_edit_value_changed(float value)
    {
        root.OnVoxelEditSizeChanged(value);
    }
    
    /**
     * Signals selection of editing voxel type to root.
     * Changes highlighted voxel type in menu.
     */
    private void OnVoxelEditTypeItemSelected(int index, bool triggerSignal)
    {
        // set all disabled
        foreach (VoxelTypeItem item in voxelEditTypeItems)
        {
            item.SetSelected(false);
        }
        
        // set selected
        voxelEditTypeItems[index].SetSelected(true);
        
        if (triggerSignal)
        {
            root.OnVoxelEditTypeChanged(index);
        }
    }

    private void _on_worlds_button_button_up()
    {
        SwitchToWorldsSelection();
    }

    private void _on_settings_button_button_up()
    {
        SwitchToSettingsInTitle();
    }

    private void _on_exit_button_button_up()
    {
        root.OnExit();
    }

    private void _on_worlds_selection_create_new_world_button_button_up()
    {
        SwitchToNewWorld();
    }

    /**
     * Attempts to load selected saved world.
     * Menu is hidden. Actual startup and generation of the world happens from root. 
     */
    private void OnWorldSelected(WorldSaveConfig worldSaveConfig)
    {
        WorldSaveConfig actualWorldData = storageService.LoadSelectedWorld(worldSaveConfig.worldDirectoryName);
        if (actualWorldData == null)
        {
            GD.Print($"Failed to load world {worldSaveConfig.worldName} from directory {worldSaveConfig.worldDirectoryName}");
            return;
        }
        
        currentWorldSaveConfig = actualWorldData;
        SwitchToInGame();
        player.ResetPlayerSettings();
        root.ResetWorldPlayerSettings();
        root.StartWorldGeneration(currentWorldSaveConfig, false);
    }

    /**
     * Deletes selected world via storage service.
     */
    private void OnWorldDelete(string worldDirectoryName)
    {
        storageService.DeleteWorld(worldDirectoryName);
        SwitchToWorldsSelection();
    }
    
    /**
     * Signal lod distance configuration to root.
     */
    public void OnSetLodDistanceConfiguration(int lodDistance0, int lodDistance1, int lodDistance2)
    {
        root.SetLodConfiguration(lodDistance0, lodDistance1, lodDistance2);
    }

    public void OnSettingsBackButtonPressed()
    {
        if (inGame)
        {
            SwitchToPauseMenu();
        }
        else
        {
            SwitchToTitleMenu();
        }
    }

    private void _on_worlds_back_button_button_up()
    {
        SwitchToTitleMenu();
    }

    private void _on_back_from_create_button_button_up()
    {
        SwitchToWorldsSelection();
    }
    
    /**
     * Processes area age distribution from ui representation to proportionally
     * distributed set of thresholds in format for world generator.
     */
    private bool ParseAreaAgeDistributionFromUI(WorldSettings worldSettings)
    {
        int[] ageDistributionInputs = new int[8];
        ageDistributionInputs[0] = (int)worldCreationAreaAgeVeryYoungSlider.Value;
        ageDistributionInputs[1] = (int)worldCreationAreaAgeYoungSlider.Value;
        ageDistributionInputs[2] = (int)worldCreationAreaAgeYoungMediumSlider.Value;
        ageDistributionInputs[3] = (int)worldCreationAreaAgeMediumSlider.Value;
        ageDistributionInputs[4] = (int)worldCreationAreaAgeMediumOldSlider.Value;
        ageDistributionInputs[5] = (int)worldCreationAreaAgeOldSlider.Value;
        ageDistributionInputs[6] = (int)worldCreationAreaAgeVeryOldSlider.Value;
        ageDistributionInputs[7] = (int)worldCreationAreaAgeAncientSlider.Value;
        // calculate thresholds
        int sum = ageDistributionInputs.Sum();
        if (sum == 0)
        {
            return false;
        }
        float lowerLimit = WorldSettings.areaAgeThresholdsMin;
        float upperLimit = WorldSettings.areaAgeThresholdsMax;
        float step = (upperLimit - lowerLimit) / sum;
        worldSettings.areaAgeThresholds = new float[WorldSettings.areaAgeThresholdsCount];
        float accumulated = lowerLimit;
        // each threshold adds its weight
        for (int i = 0; i < worldSettings.areaAgeThresholds.Length; i++)
        {
            accumulated += ageDistributionInputs[i] * step;
            worldSettings.areaAgeThresholds[i] = (float)Math.Round(accumulated, 2);
        }

        return true;
    }
    
    /**
     * Processes area height distribution from ui representation to proportionally
     * distributed set of thresholds in format for world generator.
     */
    private bool ParseAreaHeightDistributionFromUI(WorldSettings worldSettings)
    {
        int[] heightDistributionInputs = new int[3];
        heightDistributionInputs[0] = (int)worldCreationAreaHeightLowlandSlider.Value;
        heightDistributionInputs[1] = (int)worldCreationAreaHeightHillsSlider.Value;
        heightDistributionInputs[2] = (int)worldCreationAreaHeightMountainsSlider.Value;
        // calculate thresholds
        int sum = heightDistributionInputs.Sum();
        if (sum == 0)
        {
            return false;
        }
        float lowerLimit = WorldSettings.areaHeightThresholdsMin;
        float upperLimit = WorldSettings.areaHeightThresholdsMax;
        float step = (upperLimit - lowerLimit) / sum;
        worldSettings.areaHeightThresholds = new float[WorldSettings.areaHeightThresholdsCount];
        float accumulated = lowerLimit;
        // each threshold adds its weight
        for (int i = 0; i < worldSettings.areaHeightThresholds.Length; i++)
        {
            accumulated += heightDistributionInputs[i] * step;
            worldSettings.areaHeightThresholds[i] = (float)Math.Round(accumulated, 2);
        }

        return true;
    }
    
    /**
     * Processes area water availability distribution from ui representation to proportionally
     * distributed set of thresholds in format for world generator.
     */
    private bool ParseAreaWaterAvailabilityDistributionFromUI(WorldSettings worldSettings)
    {
        int[] waterAvailabilityDistributionInputs = new int[4];
        waterAvailabilityDistributionInputs[0] = (int)worldCreationAreaWaterAvailabilityAridSlider.Value;
        waterAvailabilityDistributionInputs[1] = (int)worldCreationAreaWaterAvailabilityLowSlider.Value;
        waterAvailabilityDistributionInputs[2] = (int)worldCreationAreaWaterAvailabilityMediumSlider.Value;
        waterAvailabilityDistributionInputs[3] = (int)worldCreationAreaWaterAvailabilityHighSlider.Value;
        // calculate thresholds
        int sum = waterAvailabilityDistributionInputs.Sum();
        if (sum == 0)
        {
            return false;
        }
        float lowerLimit = WorldSettings.areaWaterAvailabilityThresholdsMin;
        float upperLimit = WorldSettings.areaWaterAvailabilityThresholdsMax;
        float step = (upperLimit - lowerLimit) / sum;
        worldSettings.areaWaterAvailabilityThresholds = new byte[WorldSettings.areaWaterAvailabilityThresholdsCount];
        float accumulated = lowerLimit;
        // each threshold adds its weight
        for (int i = 0; i < worldSettings.areaWaterAvailabilityThresholds.Length; i++)
        {
            accumulated += waterAvailabilityDistributionInputs[i] * step;
            worldSettings.areaWaterAvailabilityThresholds[i] = (byte)accumulated;
        }

        return true;
    }

    /**
     * Transforms threshold related input values from 0 - 1 low - high representation in ui input elements
     * to inverted threshold representation used by world generation, where 0 - 1 is high - low.
     */
    private float ScaleRatioInputToInvertedRange(float value, float outMin, float outMax)
    {
        float inverted = 1 - value;
        float scaled = outMin + inverted * (outMax - outMin);
        return (float)Math.Round(scaled, 3);
    }

    /**
     * Processes configured values in the ui, parses and transforms necessary fields
     * and stores the values into returned WorldSettings struct.
     */
    private WorldSettings ParseWorldSettingsFromUI()
    {
        WorldSettings worldSettings = new WorldSettings();
        worldSettings.seed = (int)worldCreationSeedSpinBox.Value;
        worldSettings.terrainChunkCountY = (byte)worldCreationChunkCountYSlider.Value;
        worldSettings.heightNoiseConfig = (HeightNoiseConfigType)worldCreationTerrainConfigSelect.Selected;
        if (!ParseAreaAgeDistributionFromUI(worldSettings))
            return null;
        if (!ParseAreaHeightDistributionFromUI(worldSettings))
            return null;
        if (!ParseAreaWaterAvailabilityDistributionFromUI(worldSettings))
            return null;
        worldSettings.overallVegetationChance = (VegChanceType)worldCreationVegetationDensitySelect.Selected;
        worldSettings.waterLilyChance = (int)worldCreationWaterLilyChanceSlider.Value;
        worldSettings.waterReedsChance = (int)worldCreationWaterReedsChanceSlider.Value;
        worldSettings.snowChance = ScaleRatioInputToInvertedRange((float)worldCreationSnowChanceSlider.Value, WorldSettings.snowChanceMin, WorldSettings.snowChanceMax);
        worldSettings.cavesEnabled = worldCreationCavesEnabledButton.IsPressed();
        worldSettings.cavesStartTerrainHeightRatio = (float)worldCreationCavesStartOffsetSlider.Value;
        worldSettings.cavesWaterLevelTerrainHeightRatio = (float)worldCreationCavesMaxWaterLevelSlider.Value;
        worldSettings.cavesSpaceAmount = ScaleRatioInputToInvertedRange((float)worldCreationCavesSpaceRatioSlider.Value, WorldSettings.cavesSpaceAmountMin, WorldSettings.cavesSpaceAmountMax);
        worldSettings.cavesTunnelsChance = ScaleRatioInputToInvertedRange((float)worldCreationCavesTunnelsRatioSlider.Value, WorldSettings.cavesTunnelsChanceMin, WorldSettings.cavesTunnelsChanceMax);
        worldSettings.cavesTunnelEntrancesChance = ScaleRatioInputToInvertedRange((float)worldCreationCavesTunnelEntrancesRatioSlider.Value, WorldSettings.cavesTunnelEntrancesChanceMin, WorldSettings.cavesTunnelEntrancesChanceMax);
        worldSettings.cavesStalactitesChance = (int)worldCreationCavesStalactitesChanceSlider.Value;
        worldSettings.cavesWaterfallChance = (int)worldCreationCavesWaterfallChanceSlider.Value;
        worldSettings.explicitWaterLevelTerrainHeightRatio = (float)worldCreationWaterPrefillTerrainHeightSlider.Value;
        worldSettings.lakeSize = (LakeSizeType)worldCreationLakeSizeSelect.Selected;
        worldSettings.maxRiverLength = (int)worldCreationMaxRiverLengthSlider.Value;
        worldSettings.waterAvailabilityStrength = (WaterAvailEffectStrengthType)worldCreationWaterAvailabilityStrengthSelect.Selected;
        worldSettings.explicitWaterAvailabilityMaxEffect = (byte)worldCreationExplicitWaterAvailabilityEffectSlider.Value;
        
        return worldSettings;
    }

    /**
     * Attempts to create new world with configured world generation parameters.
     */
    private void _on_create_new_world_button_button_up()
    {
        // load values from ui
        currentWorldSaveConfig.worldName = worldNameEdit.Text;
        WorldSettings worldSettings = ParseWorldSettingsFromUI();
        if (worldSettings == null)
        {
            alertDialog.Visible = true;
            alertDialogLabel.Text = "Invalid world settings distribution values.\nIn each category at leas one of the distribution inputs must be above 0.";
            return;
        }
        currentWorldSaveConfig.worldSettings = worldSettings;
        // generate random directory name suffix
        string worldNameSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        string worldDirectoryName = $"world_{worldNameSuffix}";
        currentWorldSaveConfig.worldDirectoryName = worldDirectoryName;
        // create world
        if (storageService.CreateWorld(currentWorldSaveConfig))
        {
            SwitchToInGame();
            player.ResetPlayerSettings();
            root.ResetWorldPlayerSettings();
            root.StartWorldGeneration(currentWorldSaveConfig, true);
        }
    }

    /**
     * Saves current world configuration. Used to update the world config file with
     * last user position in the world on world exit.
     */
    private void SaveWorldConfig()
    {
        Vector3Double playerPosition = player.GetRealPosition();
        currentWorldSaveConfig.lastPlayerPosition = playerPosition;
        storageService.SaveCurrentWorldConfig(currentWorldSaveConfig);
        alreadySavedOnExit = true;
    }

    private void _on_pause_back_button_button_up()
    {
        SwitchToInGame();
    }

    private void _on_pause_settings_button_button_up()
    {
        SwitchToSettingsInGame();
    }

    // WORLD EXIT SCENARIOS
    
    private void _on_pause_main_menu_button_button_up()
    {
        SaveWorldConfig();
        SwitchToTitleMenu();
        root.OnExitToTitleMenu();
    }

    private void _on_pause_exit_button_button_up()
    {
        SaveWorldConfig();
        root.OnExit();
    }
    
    private void _on_tree_exiting()
    {
        if (inGame && !alreadySavedOnExit)
        {
            SaveWorldConfig();
        }
        root.OnExit();
    }
    
    /**
     * Populate world creation ui with values of selected preset.
     */
    private void _on_world_profile_select_item_selected(int index)
    {
        FillNewWorldUIPresetValues(index);
    }

    private void _on_alert_message_collapse_button_button_up()
    {
        alertDialog.Visible = false;
    }
}
