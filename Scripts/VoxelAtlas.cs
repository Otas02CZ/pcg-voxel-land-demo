// FILE: VoxelAtlas.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains voxel atlas class

using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Static voxel atlas class matching voxel types with predefined color palette.
 * Generates voxel atlas and allows conversion of voxel type to atlas uv.
 */
public static class VoxelAtlas
{
    private const int maxVoxelTypes = 34;
    private const int textureSize = 1;
    private const int typesPerRow = 4;
    private static int atlasWidth => typesPerRow * textureSize;
    private static int atlasHeight => ((maxVoxelTypes + typesPerRow - 1) / typesPerRow) * textureSize;
    
    // color palette
    private static Color[] typeColors = [
            Color.Color8(0,0,0), // AIR - not used
            Color.Color8(0,154,68), // GRASS - Irish Green
            Color.Color8(0,154,47), // GROUND GRASS DARK - Emerald Green
            Color.Color8(0,154,23), // GROUND GRASS DARKER - Grass Green
            Color.Color8(78,91,49), // GROUND GRASS BROWN - Army Green
            Color.Color8(64, 74, 40),
            Color.Color8(155,148,95), // GROUND GRASS BROWN YELLOW - Avocado Green
            Color.Color8(127, 81, 18), // DIRT - Medium Brown
            Color.Color8(63, 48, 29), // WOOD - Hickory Brown
            Color.Color8(113,169,44), // PLANTS - Corn Green
            Color.Color8(0, 103, 39), // PLANTS DARK - La Salle Green
            Color.Color8(34, 139, 34), // DECIDUOUS TREES - Forest Green
            Color.Color8(0, 103, 39), // CONIFER LEAVES - La Salle Green
            Color.Color8(244, 164, 96), // SAND - Sandy Brown
            Color.Color8(0, 0, 225, 200), // WATER - blue
            Color.Color8(128, 128, 128), // STONE - Gray
            Color.Color8(57, 57, 62), // STONE DARK - Dark Gray
            Color.Color8(35, 35, 41), // STONE DARKER - Very Dark Gray
            Color.Color8(144, 119, 100), // DRIPSTONE
            Color.Color8(113, 84, 79), // DRIPSTONE DARK
            Color.Color8(156, 138, 107), // STALACTITE
            Color.Color8(255,250, 250), // SNOW - Snow White
            Color.Color8(255, 0, 0), // RED - for foliage
            Color.Color8(15, 0, 255), // BLUE - for foliage
            Color.Color8(255, 255, 0), // YELLOW - for foliage
            Color.Color8(255, 255, 255), // WHITE - for foliage
            Color.Color8(127, 81, 18), // BROWN - for foliage
            // plants in water voxel variants
            Color.Color8(113,169,44), // PLANTS - Corn Green
            Color.Color8(0, 103, 39), // PLANTS DARK - La Salle Green
            Color.Color8(255, 255, 0), // YELLOW - for foliage
            Color.Color8(127, 81, 18), // BROWN - for foliage
            Color.Color8(255, 0, 0), // RED - for foliage
            Color.Color8(15, 0, 255), // BLUE - for foliage
            Color.Color8(255, 255, 255), // WHITE - for foliage
        ];
    
    private static Vector2[] uvCache = new Vector2[maxVoxelTypes];
    
    /**
     * Returns color of given voxel type.
     */
    public static Color GetVoxelTypeColor(VoxelType type)
    {
        return typeColors[(int)type];
    }

    /**
     * Generates texture atlas from color palette.
     */
    public static Texture2D GenerateTextureAtlas()
    {
        var image = Image.CreateEmpty(atlasWidth, atlasHeight, false, Image.Format.Rgb8);

        for (int typeIndex = 0; typeIndex < maxVoxelTypes; typeIndex++)
        {
            int typeX = typeIndex % typesPerRow;
            int typeY = typeIndex / typesPerRow;
            int startX = typeX * textureSize;
            int startY = typeY * textureSize;
            Color typeColor = typeColors[typeIndex];
            for (int x = 0; x < textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    image.SetPixel(startX + x, startY + y, typeColor);
                }
            }
        }
        
        ImageTexture texture = new();
        texture.SetImage(image);
        return texture;
    }
    
    /**
     * Precomputes and stores uv coordinates for faster access.
     */
    public static void PrecomputeUVs()
    {
        for (int typeIndex = 0; typeIndex < maxVoxelTypes; typeIndex++)
        {
            int typeX = typeIndex % typesPerRow;
            int typeY = typeIndex / typesPerRow;
            float u = (typeX + 0.5f) * textureSize / atlasWidth;
            float v = (typeY + 0.5f) * textureSize / atlasHeight;
            uvCache[typeIndex] = new Vector2(u, v);
        }
    }

    /**
     * Returns uv coordinate into texture atlas for given voxel type.
     */
    public static Vector2 GetAtlasUV(VoxelType type)
    {
        return uvCache[(int)type];
    }
}
