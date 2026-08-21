// FILE: VoxelTypeDropUI.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the ui voxel type drop slot component

using Godot;
using System;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents a single droppable slot for voxel type ui elements.
 * Contains (displays) VoxelTypeInnerUI which shows the voxel type visually.
 */
public partial class VoxelTypeDropUI : TextureRect
{
    private int slotIndex;
    private VoxelTypeUI voxelTypeUI;
    private VoxelTypeInnerUI voxelTypeInner;
    
    private Action<int, VoxelTypeUI> onDraggableDropped;

    public override void _Ready()
    {
        voxelTypeInner = GetNode<VoxelTypeInnerUI>("VoxelTypeInnerUI");
    }

    /**
     * Checks that only elements of type VoxelTypeDragUI can be dropped.
     */
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.Obj is VoxelTypeDragUI;
    }

    /**
     * Handles the voxel type drop signal.
     * Configures its child voxel type display.
     */
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        VoxelTypeDragUI voxelTypeDragUI = (VoxelTypeDragUI)data;
        voxelTypeUI = voxelTypeDragUI.type;
        voxelTypeInner.SetVoxelType(voxelTypeDragUI.type);
        onDraggableDropped?.Invoke(slotIndex, voxelTypeUI);
    }
    
    /**
     * Subscribes the handler to successful drop events.
     */
    public void SubscribeOnDraggableDropped(Action<int, VoxelTypeUI> handler)
    {
        onDraggableDropped += handler;
    }

    /**
     * Initializes the component with given voxel type and slot index.
     */
    public void Initialize(VoxelTypeUI voxelTypeUI, int slotIndex)
    {
        this.slotIndex = slotIndex;
        this.voxelTypeUI = voxelTypeUI;
        voxelTypeInner.SetVoxelType(voxelTypeUI);
    }
}
