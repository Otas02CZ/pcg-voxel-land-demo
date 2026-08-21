// FILE: InGame.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the in game GUI Menu

using System;
using Godot;

using PCGVoxelLandscapes.Scripts;

/**
 * Handles control of the in-game menu.
 * Consists of movement type display and simple voxel menu bar with info label.
 */
public partial class InGame : Control
{
    // regular ui components
    private Label moveModeLabel;
    private Node2D crosshair;
    private HBoxContainer voxelTypesContainer;
    private Label editingInfoLabel;
    // preloaded voxel type menu component
    private static readonly PackedScene _voxelTypeMenuBarUI = GD.Load<PackedScene>("res://Scenes/VoxelTypeMenuBarUI.tscn");
    // current config of viewable voxel types and their ui nodes
    private VoxelTypeUI[] selectedVoxelTypes;
    private VoxelTypeMenuBarUI[] selectedVoxelTypesUINodes;
    private int indexCurrentVoxelType;

    private int currentEditingVoxelSize = 1;
    private const int editingVoxelSizeMin = 1;
    private const int editingVoxelSizeMax = 7;

    private Action<int> onVoxelTypeSelected;
    private Action<int> onVoxelSizeChange;
    
    public override void _Ready()
    {
        moveModeLabel = GetNode<Label>("MoveMode/MoveModeLabel");
        crosshair = GetNode<Node2D>("Center/Crosshair");
        voxelTypesContainer = GetNode<HBoxContainer>("VoxelEditDisplay/VoxelEditMenuBarPanel/VoxelEditMenuBar");
        editingInfoLabel = GetNode<Label>("VoxelEditDisplay/PanelContainer/EditingInfoLabel");
    }

    /**
     * Engine per-frame invocation.
     * Handles user events leading to voxel type selection and size changes.
     */
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) // do not handle events when not active
        {
            return;
        }
        
        // voxel type selection
        if (Input.IsActionJustPressed("voxel_item_previous"))
        {
            indexCurrentVoxelType--;
            if (indexCurrentVoxelType == -1)
            {
                indexCurrentVoxelType = selectedVoxelTypes.Length - 1;
            }

            ProcessVoxelItemSelected();
        }
        if (Input.IsActionJustPressed("voxel_item_next"))
        {
            indexCurrentVoxelType++;
            if (indexCurrentVoxelType == selectedVoxelTypes.Length)
            {
                indexCurrentVoxelType = 0;
            }
            ProcessVoxelItemSelected();
        }
        // voxel size changes
        if (Input.IsActionJustPressed("voxel_size_decrease"))
        {
            if (currentEditingVoxelSize != editingVoxelSizeMin)
            {
                currentEditingVoxelSize--;
            }
            ProcessVoxelSizeChange();
        }
        
        if (Input.IsActionJustPressed("voxel_size_increase"))
        {
            if (currentEditingVoxelSize != editingVoxelSizeMax)
            {
                currentEditingVoxelSize++;
            }
            ProcessVoxelSizeChange();
        }
        
        if (Input.IsActionJustPressed("voxel_size_increase_overflow"))
        {
            currentEditingVoxelSize++;
            if (currentEditingVoxelSize == editingVoxelSizeMax + 1)
            {
                currentEditingVoxelSize = editingVoxelSizeMin;
            }
            ProcessVoxelSizeChange();
        }
    }

    /**
     * Handles voxel size change.
     * Updates label, notifies subscribers.
     */
    private void ProcessVoxelSizeChange()
    {
        UpdateEditingInfoLabel();
        onVoxelSizeChange?.Invoke(currentEditingVoxelSize);
    }

    /**
     * Handles voxel item selection.
     * Updates ui, notifies parent.
     */
    private void ProcessVoxelItemSelected()
    {
        if (selectedVoxelTypesUINodes == null)
        {
            return;
        }
        
        for (int index = 0; index < selectedVoxelTypesUINodes.Length; index++)
        {
            selectedVoxelTypesUINodes[index].SetSelected(index == indexCurrentVoxelType);
        }
        
        UpdateEditingInfoLabel();
        onVoxelTypeSelected?.Invoke(indexCurrentVoxelType);
    }

    /**
     * Sets up the component.
     */
    public void Setup(Player player, VoxelTypeUI[] selectedVoxelTypes, int currentIndex, int currentEditingVoxelSize)
    {
        SetMovementMode(player.GetMovementMode());
        player.SubscribeOnMovementModeChanged(SetMovementMode);
        this.selectedVoxelTypes = selectedVoxelTypes;
        this.currentEditingVoxelSize = currentEditingVoxelSize;
        indexCurrentVoxelType = currentIndex;
        FillSelectedVoxelTypesContainer();
    }

    /**
     * Updates the info label depending on current component state.
     */
    private void UpdateEditingInfoLabel()
    {
        if (selectedVoxelTypes == null)
        {
            return;
        }
        
        // assemble and set info label
        int actualVoxelSize = (int)Math.Pow(2, currentEditingVoxelSize - 1);
        editingInfoLabel.SetText($"{selectedVoxelTypes[indexCurrentVoxelType].name} - size: {actualVoxelSize}x{actualVoxelSize}x{actualVoxelSize}");
    }

    /**
     * Fills the hotbar with selected voxel type items.
     */
    private void FillSelectedVoxelTypesContainer()
    {
        if (selectedVoxelTypesUINodes != null)
        {
            for (int index = 0; index < selectedVoxelTypesUINodes.Length; index++)
            {
                if (selectedVoxelTypesUINodes[index] != null)
                {
                    selectedVoxelTypesUINodes[index].QueueFree();
                    selectedVoxelTypesUINodes[index] = null;
                }
            }
        }
        
        selectedVoxelTypesUINodes = new VoxelTypeMenuBarUI[selectedVoxelTypes.Length];
        for (int index = 0; index < selectedVoxelTypes.Length; index++)
        {
            VoxelTypeUI voxelTypeUI = selectedVoxelTypes[index];
            VoxelTypeMenuBarUI item = _voxelTypeMenuBarUI.Instantiate<VoxelTypeMenuBarUI>();
            voxelTypesContainer.AddChild(item);
            item.Initialize(voxelTypeUI);
            item.SetSelected(index == indexCurrentVoxelType);
            selectedVoxelTypesUINodes[index] = item;
        }

        UpdateEditingInfoLabel();
    }

    /**
     * Updates voxel type in indexed slot, together with its display in the hotbar.
     */
    public void UpdateSelectedVoxelTypeSlot(int index, VoxelTypeUI type)
    {
        selectedVoxelTypes[index] = type;
        selectedVoxelTypesUINodes[index].Initialize(type);
        ProcessVoxelItemSelected();
    }

    /**
     * Updates voxel editing size. Changes info label.
     */
    public void UpdateVoxelEditingSize(int editingSize)
    {
        currentEditingVoxelSize = editingSize;
        UpdateEditingInfoLabel();
    }
    
    /**
     * Sets movement mode display label.
     */
    private void SetMovementMode(MovementMode mode)
    {
        moveModeLabel.Text = mode == MovementMode.FLY ? "Flying" : "Walking";
    }

    /**
     * Sets visibility of in-game crosshair.
     */
    public void SetCrosshairVisibility(bool visible)
    {
        crosshair.Visible = visible;
    }

    /**
     * Subscribes to signal informing about new voxel type selection.
     */
    public void SubscribeOnVoxelTypeSelected(Action<int> handler)
    {
        onVoxelTypeSelected += handler;
    }
    
    /**
     * Subscribes to signal informing about change of editing voxel size.
     */
    public void SubscribeOnVoxelEditingSizeChanged(Action<int> handler)
    {
        onVoxelSizeChange += handler;
    }
}
