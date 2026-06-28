// FILE: TrunkGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains TrunkGenerator class allowing generation of fallen tree trunks. Internally uses TreeGenerator.

namespace PCGVoxelLandscapes.Scripts;

/**
 * Possible trunk rotations in the XZ plane.
 */
public enum TrunkRotation : byte
{
    X_POSITIVE,
    X_NEGATIVE,
    Z_POSITIVE,
    Z_NEGATIVE
}

/**
 * Structure holding parameters for TrunkGenerator.
 */
public struct TrunkDefinition
{
    public TreeDefinition treeDefinition;
    public TrunkRotation rotationXZ;
    
    public TrunkDefinition(TreeDefinition treeDefinition, TrunkRotation rotationXZ)
    {
        this.treeDefinition = treeDefinition;
        this.treeDefinition.maxLeafRadius = 0f;
        this.treeDefinition.leafRadiusScale = 0f;
        this.rotationXZ = rotationXZ;
    }
}

/**
 * Generated Trunk model.
 */
public class Trunk(VoxelItem[] voxels, short offsetY, int id)
{
    public readonly int id = id;
    public readonly VoxelItem[] voxels = voxels;
    public readonly short offsetY = offsetY;
}

/**
 * Allows generation of fallen tree trunks.
 * Uses TreeGenerator to generate a tree, which is then transformed into fallen position in XZ plane
 * and rotated to be positioned in +X, -X, +Z, -Z direction.
 */
public static class TrunkGenerator
{
    /**
     * Generates single trunk voxel model.
     * Internally uses TreeGenerator. Generated tree voxel model is transformed into fallen position in XZ plane
     * and then rotated to one of defined rotation positions: +X, -X, +Z, -Z
     */
    public static (VoxelItem[], short offsetY) Generate(TrunkDefinition trunkDef)
    {
        // generate tree model for the trunk
        TreeGenerator treeGen = new TreeGenerator(
            trunkDef.treeDefinition.nAttractionPoints,
            trunkDef.treeDefinition.maxIterations,
            trunkDef.treeDefinition.influenceRadius,
            trunkDef.treeDefinition.killDistance,
            trunkDef.treeDefinition.branchLength,
            trunkDef.treeDefinition.growthBias,
            trunkDef.treeDefinition.crownOffset,
            trunkDef.treeDefinition.envelopeType,
            trunkDef.treeDefinition.envelopeRadius,
            trunkDef.treeDefinition.envelopeHeight,
            trunkDef.treeDefinition.seed,
            trunkDef.treeDefinition.kTop,
            trunkDef.treeDefinition.kBottom,
            trunkDef.treeDefinition.radialBias,
            trunkDef.treeDefinition.verticalBias,
            trunkDef.treeDefinition.interiorMin,
            trunkDef.treeDefinition.coniferWedgeSpacing,
            trunkDef.treeDefinition.coniferWedgeHeight,
            trunkDef.treeDefinition.offsetY,
            trunkDef.treeDefinition.basicRadius,
            trunkDef.treeDefinition.radiusExponent
        );
        treeGen.Generate();
        // voxelize and obtain the model
        VoxelModel trunkModel = treeGen.GetVoxelModel(trunkDef.treeDefinition.trunk, trunkDef.treeDefinition.leaves);
        VoxelItem[] voxels = trunkModel.GetVoxelItems();

        // transform the tree to fallen position, from (x, y, z) to (y, -x, z) along X axis
        short minX = short.MaxValue, maxX = short.MinValue;
        short minY = short.MaxValue, maxY = short.MinValue;
        short minZ = short.MaxValue, maxZ = short.MinValue;

        for (int index = 0; index < voxels.Length; index++)
        {
            VoxelItem voxel = voxels[index];
            
            short newX = voxel.Y;
            short newY = (short)(-voxel.X);
            short newZ = voxel.Z;

            voxel.X = newX;
            voxel.Y = newY;
            voxel.Z = newZ;

            voxels[index] = voxel;
            
            // update limits
            if (newX < minX) minX = newX;
            if (newX > maxX) maxX = newX;
            if (newY < minY) minY = newY;
            if (newY > maxY) maxY = newY;
            if (newZ < minZ) minZ = newZ;
            if (newZ > maxZ) maxZ = newZ;
        }
        
        // calculate center offsets
        short xOffset = (short)(-(minX + maxX) / 2);
        short yOffset = (short)(-(minY + maxY) / 2);
        short zOffset = (short)(-(minZ + maxZ) / 2);
        
        // center and rotate voxels to +X, -X, +Z, or -Z
        for (int index = 0; index < voxels.Length; index++)
        {
            VoxelItem voxel = voxels[index];
            
            // center the voxel
            short x = (short)(voxel.X + xOffset);
            short y = (short)(voxel.Y + yOffset);
            short z = (short)(voxel.Z + zOffset);
            
            // apply rotation
            (x, y, z) = ApplyDirectionalRotation(x, y, z, trunkDef.rotationXZ);

            voxel.X = x;
            voxel.Y = y;
            voxel.Z = z;
            
            voxels[index] = voxel;
        }
        
        return (voxels, trunkDef.treeDefinition.offsetY);
    }
    
    /**
     * Rotates centered coordinates based on rotation configuration.
     */
    private static (short x, short y, short z) ApplyDirectionalRotation(short x, short y, short z, TrunkRotation rotation)
    {
        return rotation switch
        {
            TrunkRotation.X_POSITIVE => (x, y, z),                  // default
            TrunkRotation.X_NEGATIVE => ((short)-x, y, (short)-z),  // 180 deg around Y-axis
            TrunkRotation.Z_POSITIVE => ((short)-z, y, x),          // 90 deg around Y-axis
            TrunkRotation.Z_NEGATIVE => (z, y, (short)-x),          // -90 deg around Y-axis
            _ => (x, y, z)
        };
    }
}
