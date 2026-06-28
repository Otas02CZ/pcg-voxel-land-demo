// FILE: VoxelTypeItem.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains class of GUI node for display of one voxel type

using System;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * GUI node of a clickable voxel type for selection of current voxel type for editing.
 */
public partial class VoxelTypeItem : TextureRect
{
    private TextureButton textureButton;
    // shared background of the button informing whether it is selected or not
    private static ImageTexture _textureSelected;
    private static ImageTexture _textureUnselected;
    
    /**
     * Initializes the shared background textures if it is the first node.
     */
    public override void _Ready()
    {
        textureButton = GetNode<TextureButton>("TextureButton");
        
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
     * Sets up button press signal
     */
    public void WireUpOnClicked(Action<int, bool> onClicked, int voxelTypeIndex)
    {
        textureButton.Pressed += () => onClicked(voxelTypeIndex, true);
    }
    
    /**
     * Set color of this button based on given voxel type.
     */
    public void SetVoxelType(VoxelType voxelType)
    {
        Color color = VoxelAtlas.GetVoxelTypeColor(voxelType);
        Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, color);
        ImageTexture texture = new();
        texture.SetImage(image);
        textureButton.TextureNormal = texture;
        textureButton.TextureHover = texture;
        textureButton.TexturePressed = texture;
        TooltipText = voxelType.ToString();
        textureButton.TooltipText = TooltipText;
    }
    
    /**
     * Toggle between selected / unselected
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
}
