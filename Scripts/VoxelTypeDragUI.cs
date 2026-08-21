// FILE: VoxelTypeDragUI.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the ui voxel type drag item component

using Godot;

using PCGVoxelLandscapes.Scripts;

/**
 * Represents single draggable voxel type ui component.
 */
public partial class VoxelTypeDragUI : TextureRect
{
    private VoxelTypeInnerUI voxelTypeInner;
    public VoxelTypeUI type {get; private set;}

    public override void _Ready()
    {
        voxelTypeInner = GetNode<VoxelTypeInnerUI>("VoxelTypeInnerUI");
    }

    /**
     * Configures the draggable preview and returns itself as the dragged component.
     */
    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview((VoxelTypeInnerUI)voxelTypeInner.Duplicate());
        return this;
    }

    /**
     * Initializes this component to the specified voxel type.
     */
    public void Initialize(VoxelTypeUI voxelTypeUI)
    {
        type = voxelTypeUI;
        voxelTypeInner.SetVoxelType(voxelTypeUI);
    }
}
