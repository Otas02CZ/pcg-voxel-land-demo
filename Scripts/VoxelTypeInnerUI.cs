// FILE: VoxelTypeInnerUI.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Represents inner component of ui voxel type, which displays it visually.

using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents inner part of the voxel type ui component.
 * Displays the voxel type via shader material
 */
public partial class VoxelTypeInnerUI : TextureRect
{
    // preload the material
    private static ShaderMaterial material = GD.Load<ShaderMaterial>("res://Materials/UI2DBlockMaterial.tres");
    
    /**
     * Configures the component to display the specified voxel type.
     */
    public void SetVoxelType(VoxelTypeUI voxelTypeUI)
    {
        Texture2D voxelAtlas = VoxelAtlas.GetTextureAtlas();
        Vector2 atlasUV = VoxelAtlas.GetAtlasUV(voxelTypeUI.type);
        // duplicate the material and set voxel type specific parameters
        ShaderMaterial specificMaterial = (ShaderMaterial)material.Duplicate(true);
        specificMaterial.SetShaderParameter("atlas_texture", voxelAtlas);
        specificMaterial.SetShaderParameter("atlas_uv", atlasUV);
        
        SetMaterial(specificMaterial);
        TooltipText = voxelTypeUI.name;
    }
}
