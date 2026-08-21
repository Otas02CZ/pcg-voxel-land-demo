// FILE: VoxelTypeMenuBarUI.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Handling code for the voxel type menu bar (hotbar) ui component

using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents single voxel type ui component of the hotbar.
 */
public partial class VoxelTypeMenuBarUI : TextureRect
{
    private VoxelTypeInnerUI voxelTypeInner;
    
    // shared background of the button informing whether it is selected or not
    private static ImageTexture _textureSelected;
    private static ImageTexture _textureUnselected;

    public override void _Ready()
    {
        voxelTypeInner = GetNode<VoxelTypeInnerUI>("VoxelTypeInnerUI");
        // initialize textures in the first instance
        if (_textureSelected == null)
        {
            Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.SetPixel(0, 0, Color.Color8(255, 255, 0)); // yellow
            _textureSelected = new ImageTexture();
            _textureSelected.SetImage(image);
        }
        if (_textureUnselected == null)
        {
            Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.SetPixel(0, 0, Color.Color8(39, 39, 39)); // gray
            _textureUnselected = new ImageTexture();
            _textureUnselected.SetImage(image);
        }
    }
    
    /**
     * Toggle between selected / unselected state.
     */
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            SetTexture(_textureSelected);
        }
        else
        {
            SetTexture(_textureUnselected);
        }
    }
    
    /**
     * Initializes the component with the specified voxel type.
     */
    public void Initialize(VoxelTypeUI voxelTypeUI)
    {
        voxelTypeInner.SetVoxelType(voxelTypeUI);
    }
}
