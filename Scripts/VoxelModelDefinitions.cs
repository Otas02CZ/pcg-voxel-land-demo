// FILE: VoxelModelDefinitions.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains static class that can fill model generator with predefined model definitions

using System.Numerics;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Filler of model definitions.
 */
public static class VoxelModelDefinitions
{
	/**
	 * Fills supplied model generator with predefined model definitions.
	 */
    public static void InsertVoxelModelDefinitions(ModelService modelService)
    {
        const float maxLeafRadius = 0.39f;
		const float leafRadiusScale = 3.2f;

		const float maxLeafRadiusWeak = 0.38f;
		const float leafRadiusScaleWeak = 0.8f;
		
		// Trees deciduous
		// SMALL HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 90, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 15+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 75, 25f, 0.4f, 0.50f, new Vector3(-0.2f, -0.2f, -0.2f), 10+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 90, 50f, 0.9f, 0.5f, new Vector3(0.2f, 0.2f, 0.2f), 13+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 80, 40f, 0.25f, 0.75f, new Vector3(0.2f, -0.2f, 0.2f), 11+8, EnvelopeType.OVAL, 16f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 240, 50, 35f, 0.6f, 0.85f, new Vector3(0.2f, 0.2f, -0.2f), 11+8, EnvelopeType.SPHERE, 16f, 32f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// SMALL WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 240, 70, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 15+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 260, 80, 30f, 0.6f, 0.60f, new Vector3(-0.2f, -0.2f, -0.2f), 10+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 240, 75, 50f, 1f, 0.65f, new Vector3(0.2f, 0.2f, 0.2f), 11+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 220, 80, 40f, 0.7f, 0.8f, new Vector3(0.2f, -0.2f, 0.2f), 11+8, EnvelopeType.OVAL, 12f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// SMALL DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 160, 35, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 13+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 200, 35, 40f, 2f, 1f, new Vector3(-0.2f, -0.2f, -0.2f), 8+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 180, 35, 50f, 2f, 1f, new Vector3(0.2f, 0.2f, 0.2f), 10+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 180, 30, 40f, 2f, 1f, new Vector3(0.2f, -0.2f, 0.2f), 8+8, EnvelopeType.OVAL, 16f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		
		// MEDIUM HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 650, 160, 45f, 0.25f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 520, 200, 60f, 2f, 1f, new Vector3(-0.2f, -0.2f, -0.2f), 15+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 600, 210, 90f, 2f, 1f, new Vector3(0.2f, 0.2f, 0.2f), 20+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 500, 180, 55f, 1.4f, 1f, new Vector3(0.2f, -0.2f, -0.2f), 15+8, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 500, 200, 50f, 0.8f, 0.5f, new Vector3(-0.2f, -0.2f, 0.2f), 18+8, EnvelopeType.CYLINDER, 26f, 80f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 600, 200, 60f, 0.50f, 0.50f, new Vector3(0.2f, -0.2f, 0.2f), 18+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 600, 190, 60f, 0.50f, 0.50f, new Vector3(0.2f, -0.2f, 0.2f), 20+8, EnvelopeType.PARABOLOID_CAP, 35f, 65f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
        
		// MEDIUM WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 650, 160, 45f, 0.25f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 520, 200, 60f, 2f, 1f, new Vector3(-0.2f, -0.2f, -0.2f), 15+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 500, 200, 90f, 2f, 1f, new Vector3(0.2f, 0.2f, 0.2f), 20+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 500, 180, 55f, 1.4f, 1f, new Vector3(0.2f, -0.2f, -0.2f), 15+8, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 500, 200, 50f, 0.8f, 0.5f, new Vector3(-0.2f, -0.2f, 0.2f), 18+8, EnvelopeType.CYLINDER, 26f, 80f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 600, 200, 60f, 0.50f, 0.50f, new Vector3(0.2f, -0.2f, 0.2f), 18+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 600, 190, 60f, 0.50f, 0.50f, new Vector3(0.2f, -0.2f, 0.2f), 18+8, EnvelopeType.PARABOLOID_CAP, 35f, 65f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// MEDIUM DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 400, 35, 40f, 2f, 1f, new Vector3(-0.2f, -0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.65f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 350, 40, 60f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.75f, radiusExponent: 2.8f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 400, 45, 90f, 2f, 1f, new Vector3(0.2f, -0.2f, -0.2f), 16+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.75f, radiusExponent: 2.8f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 300, 35, 45f, 2f, 1f, new Vector3(0.2f, 0.2f, 0.2f), 15+8, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.75f, radiusExponent: 2.8f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 500, 55, 50f, 2f, 1f, new Vector3(0.2f, -0.2f, 0.2f), 16+8, EnvelopeType.CYLINDER, 24f, 100f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.65f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 400, 55, 60f, 2f, 1f, new Vector3(-0.2f, -0.2f, 0.2f), 21+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.75f, radiusExponent: 2.8f));
		
		// LARGE HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1500, 310, 45f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 27+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1300, 280, 45f, 1f, 0.5f, new Vector3(0.2f, 0.2f, 0.2f), 25+8, EnvelopeType.OVAL, 35f, 150f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1300, 330, 50f, 1f, 0.5f, new Vector3(-0.2f, -0.2f, -0.2f), 27+8, EnvelopeType.CYLINDER, 45f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1650, 275, 50f, 1f, 0.5f, new Vector3(0.1f, -0.1f, 0.1f), 27+8, EnvelopeType.PARABOLOID_CAP, 57f, 105f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// LARGE WEAK
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1200, 235, 50f, 2f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 24+8, EnvelopeType.SPHERE, 55f, 110f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1100, 210, 50f, 2f, 0.75f, new Vector3(0.2f, 0.2f, 0.2f), 23+8, EnvelopeType.OVAL, 35f, 150f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1100, 230, 50f, 2f, 0.75f, new Vector3(-0.2f, -0.2f, -0.2f), 24+8, EnvelopeType.CYLINDER, 45f, 110f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1250, 215, 50f, 2f, 0.75f, new Vector3(0.1f, -0.1f, 0.1f), 24+8, EnvelopeType.PARABOLOID_CAP, 57f, 105f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// LARGE DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.DEAD, 650, 100, 60f, 2f, 0.75f, new Vector3(0.1f, 0.1f, -0.1f), 30+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 1.4f, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.DEAD, 650, 80, 60f, 2f, 0.75f, new Vector3(0.1f, -0.1f, 0.1f), 30+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 1.3f, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.DEAD, 700, 125, 60f, 2f, 0.6f, new Vector3(-0.1f, -0.1f, -0.1f), 30+8, EnvelopeType.SPHERE, 55f, 110f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 1.1f, radiusExponent: 2.7f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.DEAD, 800, 160, 60f, 2f, 0.6f, new Vector3(-0.1f, -0.1f, 0.1f), 30+8, EnvelopeType.SPHERE, 55f, 110f,
            trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 1.0f, radiusExponent: 2.7f));
		
		// Trees coniferous
		// LARGE HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1600, 250, 6f, 0.8f, 0.6f, new Vector3(0.4f, -0.2f, -0.3f), 60+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1500, 250, 6f, 0.85f, 0.65f, new Vector3(-0.4f, 0.2f, 0.3f), 70+8, EnvelopeType.CONIFER_WEDGE, 40f, 180f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1050, 215, 6f, 0.9f, 0.75f, new Vector3(0.2f, 0.2f, 0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1100, 220, 6f, 0.9f, 0.75f, new Vector3(-0.2f, -0.2f, -0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 180f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 900, 195, 75f, 2.1f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 25+8, EnvelopeType.CONE, 30f, 175f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1000, 195, 75f, 2.1f, 1f, new Vector3(0.2f, -0.2f, 0.2f), 25+8, EnvelopeType.CONE, 35f, 160f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// LARGE WEAK
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 1450, 205, 6f, 0.9f, 0.7f, new Vector3(0.4f, -0.2f, -0.3f), 60+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 1450, 205, 6f, 0.9f, 0.725f, new Vector3(-0.4f, 0.2f, 0.3f), 70+8, EnvelopeType.CONIFER_WEDGE, 40f, 180f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 975, 170, 6f, 1f, 0.8f, new Vector3(0.2f, 0.2f, 0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 1020, 175, 6f, 1f, 0.8f, new Vector3(-0.2f, -0.2f, -0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 180f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 875, 180, 75f, 2.1f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 25+8, EnvelopeType.CONE, 30f, 175f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 950, 180, 75f, 2.1f, 1f, new Vector3(0.2f, -0.2f, 0.2f), 25+8, EnvelopeType.CONE, 35f, 160f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
        
        // LARGE DEAD
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.DEAD, 1200, 140, 6f, 2f, 1f, new Vector3(0.4f, -0.2f, -0.3f), 60+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.DEAD, 1300, 140, 6f, 2f, 1f, new Vector3(-0.4f, 0.2f, 0.3f), 70+8, EnvelopeType.CONIFER_WEDGE, 40f, 165f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.DEAD, 900, 120, 6f, 2f, 1f, new Vector3(0.2f, 0.2f, 0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 170f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.DEAD, 900, 120, 6f, 2f, 1f, new Vector3(-0.2f, -0.2f, -0.4f), 23+8, EnvelopeType.CONIFER_WEDGE, 40f, 165f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8, basicRadius: 0.55f, radiusExponent: 2.9f));
		
		// MEDIUM HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 850, 150, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 45+8, EnvelopeType.CONIFER_WEDGE, 30f, 125f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 800, 155, 8f, 0.9f, 0.7f, new Vector3(-0.4f, 0.2f, 0.3f), 43+8, EnvelopeType.CONIFER_WEDGE, 30f, 120f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 550, 150, 8f, 1f, 0.75f, new Vector3(-0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 30f, 125f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 500, 155, 8f, 0.9f, 0.7f, new Vector3(0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 30f, 120f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 420, 160, 70f, 2f, 1f, new Vector3(-0.2f, -0.2f, 0.2f), 20+8, EnvelopeType.CONE, 25f, 90f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 390, 150, 70f, 2f, 1f, new Vector3(0.2f, 0.2f, -0.2f), 18+8, EnvelopeType.CONE, 25f, 85f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8, radiusExponent: 2.6f));
        
		// MEDIUM WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 650, 135, 8f, 1.15f, 0.85f, new Vector3(0.4f, -0.2f, -0.3f), 45+8, EnvelopeType.CONIFER_WEDGE, 30f, 125f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 600, 138, 8f, 1.1f, 0.8f, new Vector3(-0.4f, 0.2f, 0.3f), 43+8, EnvelopeType.CONIFER_WEDGE, 30f, 120f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 420, 130, 8f, 1.15f, 0.85f, new Vector3(-0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 30f, 125f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 400, 132, 8f, 1.1f, 0.8f, new Vector3(0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 30f, 120f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 420, 160, 70f, 2f, 1f, new Vector3(-0.2f, -0.2f, 0.2f), 20+8, EnvelopeType.CONE, 25f, 90f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 390, 150, 70f, 2f, 1f, new Vector3(0.2f, 0.2f, -0.2f), 18+8, EnvelopeType.CONE, 25f, 85f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8, radiusExponent: 2.6f));
        
        // MEDIUM DEAD
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.DEAD, 290, 84, 8f, 2f, 1f, new Vector3(0.4f, -0.2f, -0.3f), 45+8, EnvelopeType.CONIFER_WEDGE, 25f, 125f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.DEAD, 280, 83, 8f, 2f, 1f, new Vector3(-0.4f, 0.2f, 0.3f), 43+8, EnvelopeType.CONIFER_WEDGE, 25f, 120f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.DEAD, 190, 71, 8f, 2f, 1f, new Vector3(-0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 25f, 125f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.DEAD, 170, 70, 8f, 2f, 1f, new Vector3(0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 25f, 120f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        
		// SMALL HEALTHY
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 320, 100, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 300, 95, 8f, 0.9f, 0.7f, new Vector3(-0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 260, 92, 8f, 1f, 0.75f, new Vector3(-0.4f, -0.2f, -0.3f), 12+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 245, 88, 8f, 0.9f, 0.7f, new Vector3(0.4f, 0.2f, 0.3f), 13+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 235, 90, 40f, 2f, 1f, new Vector3(-0.2f, -0.2f, 0.2f), 10+8, EnvelopeType.CONE, 20f, 60f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 190, 75, 40f, 2f, 1f, new Vector3(0.2f, 0.2f, -0.2f), 11+8, EnvelopeType.CONE, 20f, 60f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8, radiusExponent: 2.6f));
		
		// SMALL WEAK
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 250, 80, 8f, 1.15f, 0.85f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 230, 75, 8f, 1f, 0.8f, new Vector3(-0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 210, 75, 8f, 1.15f, 0.85f, new Vector3(-0.4f, -0.2f, -0.3f), 12+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 195, 73, 8f, 1f, 0.8f, new Vector3(0.4f, 0.2f, 0.3f), 13+8, EnvelopeType.CONIFER_WEDGE, 20f, 80f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: 0.25f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 235, 90, 40f, 2f, 1f, new Vector3(-0.2f, -0.2f, 0.2f), 10+8, EnvelopeType.CONE, 20f, 60f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8, radiusExponent: 2.6f));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 190, 75, 40f, 2f, 1f, new Vector3(0.2f, 0.2f, -0.2f), 11+8, EnvelopeType.CONE, 20f, 60f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8, radiusExponent: 2.6f));
        
		// SMALL DEAD
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.DEAD, 105, 50, 8f, 2f, 1f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 15f, 80f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.DEAD, 97, 45, 8f, 2f, 1f, new Vector3(-0.4f, 0.2f, 0.3f), 22+8, EnvelopeType.CONIFER_WEDGE, 15f, 80f, kTop:0.8f, kBottom:0.8f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.DEAD, 75, 40, 8f, 2f, 1f, new Vector3(-0.4f, -0.2f, -0.3f), 12+8, EnvelopeType.CONIFER_WEDGE, 15f, 80f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.DEAD, 70, 38, 8f, 2f, 1f, new Vector3(0.4f, 0.2f, 0.3f), 13+8, EnvelopeType.CONIFER_WEDGE, 15f, 80f, kTop:-0.5f, kBottom:-0.5f,
            trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
        
		// Bushes
		modelService.AddBushDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.SPHERE, 15, 30, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.PARABOLOID_CAP, 15, 30, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.3f, 0.2f, 0.3f), 1f, EnvelopeType.OVAL, 30, 10, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.OVAL, 35, 14, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.PARABOLOID_CAP, 20, 20, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		
		modelService.AddBushDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.SPHERE, 15, 30, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.PARABOLOID_CAP, 15, 30, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.3f, 0.2f, 0.3f), 1f, EnvelopeType.OVAL, 30, 10, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.OVAL, 35, 14, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		modelService.AddBushDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 70, 20, 2, 1, new Vector3(-0.1f, 0.1f, -0.1f), 1f, EnvelopeType.PARABOLOID_CAP, 20, 20, radialBias:1, verticalBias:1, interiorMin:1,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2.6f));
		
		// grass
		modelService.AddGrassDefinition(new GrassDefinition(4, 12, 8, 20, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 12, 8, 20, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 12, 8, 20, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 10, 6, 18, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 10, 6, 18, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 10, 6, 18, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 12, 10, 16, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 12, 10, 16, 2, 2, VoxelType.PLANT));
        modelService.AddGrassDefinition(new GrassDefinition(4, 12, 10, 16, 2, 2, VoxelType.PLANT));
		
		// plants
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.POPPY, 2, 4));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.POPPY, 2, 3));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.POPPY, 2, 5));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.POPPY, 2, 5));
		
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BLUEBELL, 2, 4, 7, 12));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BLUEBELL, 2, 3, 12, maxStemLength:20));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BLUEBELL, 2, 5, 10, maxStemLength:16));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BLUEBELL, 2, 5, 10, maxStemLength:16));
		
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DAISY, 2, 4));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DAISY, 2, 3));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DAISY, 2, 5));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DAISY, 2, 5));
		
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DANDELION, 2, 4));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DANDELION, 2, 3));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DANDELION, 2, 5));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.DANDELION, 2, 5));
		
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.FERN, 2, 4, 3, 11, voxelType:VoxelType.PLANT_DARK));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.FERN, 2, 3, 3, 11, voxelType:VoxelType.PLANT_DARK));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.FERN, 2, 5, 6, 11, voxelType:VoxelType.PLANT_DARK));
		
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BURDOCK, 2, 7, 2, 9, voxelType:VoxelType.PLANT_DARK));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BURDOCK, 3, 8, 3, 10, voxelType:VoxelType.PLANT_DARK));
		modelService.AddPlantDefinition(new PlantDefinition(PLANT_TYPE.BURDOCK, 4, 10, 4, 11, voxelType:VoxelType.PLANT_DARK));
		
		// reeds
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 7, 4, 4, VoxelType.PLANT, VoxelType.BROWN, seedsChance:15));
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 7, 4, 4, VoxelType.PLANT, VoxelType.BROWN, seedsChance:15));
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 7, 4, 4, VoxelType.PLANT, VoxelType.BROWN, seedsChance:15));
		// reeds - kosatec
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 8, 4, 4, VoxelType.PLANT, VoxelType.YELLOW, seedsChance:10));
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 8, 4, 4, VoxelType.PLANT, VoxelType.YELLOW, seedsChance:10));
		modelService.AddReedDefinition(new ReedDefinition(9, 16, 3, 8, 4, 4, VoxelType.PLANT, VoxelType.YELLOW, seedsChance:10));
		
		// water lilies
		modelService.AddWaterLilyDefinition(new WaterLilyDefinition(VoxelType.PLANT, VoxelType.PLANT_DARK, VoxelType.YELLOW, VoxelType.WHITE, 1, 6, 3, 8));
		modelService.AddWaterLilyDefinition(new WaterLilyDefinition(VoxelType.PLANT, VoxelType.PLANT_DARK, VoxelType.BLUE, VoxelType.WHITE, 1, 6, 3, 8));
		modelService.AddWaterLilyDefinition(new WaterLilyDefinition(VoxelType.PLANT, VoxelType.PLANT_DARK, VoxelType.RED, VoxelType.WHITE, 1, 6, 3, 8));
		
		// plant bushes
		modelService.AddPlantBushDefinition(new PlantBushDefinition(maxHeight: 26));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(maxHeight: 25));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.WHITE, maxHeight: 24));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.WHITE, maxHeight: 23));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.BLUE, maxHeight: 27));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.BLUE, maxHeight: 25));
		
		// rocks
        modelService.AddRockDefinition(new RockDefinition(50, 50, 50, 25, RockType.STONE, VoxelType.STONE)); 
        modelService.AddRockDefinition(new RockDefinition(30, 30, 30, 15, RockType.STONE, VoxelType.STONE)); 
        modelService.AddRockDefinition(new RockDefinition(20, 20, 20, 10, RockType.STONE, VoxelType.STONE)); 
        modelService.AddRockDefinition(new RockDefinition(16, 16, 16, 8, RockType.STONE, VoxelType.STONE));
        
        modelService.AddRockDefinition(new RockDefinition(50, 50, 50, 25, RockType.SAND, VoxelType.SAND));
        modelService.AddRockDefinition(new RockDefinition(30, 30, 30, 15, RockType.SAND, VoxelType.SAND));
        modelService.AddRockDefinition(new RockDefinition(20, 20, 20, 10, RockType.SAND, VoxelType.SAND));
        modelService.AddRockDefinition(new RockDefinition(16, 16, 16, 8, RockType.SAND, VoxelType.SAND));
		
		// trunks and branches
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 80, 60f, 2f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 20, EnvelopeType.OVAL, 36f, 65f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:1.2f, radiusExponent:2.7f), TrunkRotation.X_POSITIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 80, 60f, 2f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 20, EnvelopeType.OVAL, 36f, 65f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:1.2f, radiusExponent:2.7f), TrunkRotation.Z_POSITIVE));
		
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 85, 60f, 2f, 0.50f, new Vector3(0.2f, 0.2f, -0.2f), 24, EnvelopeType.OVAL, 40f, 80f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:1.4f, radiusExponent:2.5f), TrunkRotation.X_POSITIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 85, 60f, 2f, 0.50f, new Vector3(0.2f, 0.2f, -0.2f), 24, EnvelopeType.OVAL, 40f, 80f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:1.4f, radiusExponent:2.5f), TrunkRotation.Z_NEGATIVE));
		
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 200, 60, 45f, 2f, 0.50f, new Vector3(-0.2f, 0.2f, 0.2f), 14, EnvelopeType.CYLINDER, 26f, 45f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.8f, radiusExponent:2.8f), TrunkRotation.X_NEGATIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 200, 60, 45f, 2f, 0.50f, new Vector3(-0.2f, 0.2f, 0.2f), 14, EnvelopeType.CYLINDER, 26f, 45f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.8f, radiusExponent:2.8f), TrunkRotation.Z_POSITIVE));
		
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 300, 25, 55f, 4f, 0.7f, new Vector3(-0.2f, 0.2f, -0.2f), 6, EnvelopeType.OVAL, 12f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.X_NEGATIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 300, 25, 55f, 4f, 0.7f, new Vector3(-0.2f, 0.2f, -0.2f), 6, EnvelopeType.OVAL, 12f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.Z_NEGATIVE));
		
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 300, 30, 60f, 4f, 0.7f, new Vector3(0.2f, 0.2f, -0.2f), 8, EnvelopeType.CYLINDER, 16f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.X_POSITIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 300, 30, 60f, 4f, 0.7f, new Vector3(0.2f, 0.2f, -0.2f), 8, EnvelopeType.CYLINDER, 16f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.Z_POSITIVE));
		
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 30, 60f, 4f, 0.7f, new Vector3(0.2f, 0.2f, -0.2f), 8, EnvelopeType.PARABOLOID_CUP, 16f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.X_NEGATIVE));
		modelService.AddTrunkDefinition(new TrunkDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 250, 30, 60f, 4f, 0.7f, new Vector3(0.2f, 0.2f, -0.2f), 8, EnvelopeType.PARABOLOID_CUP, 16f, 30f,
			trunk: VoxelType.WOOD, offsetY:0, basicRadius:0.6f, radiusExponent:2.9f), TrunkRotation.Z_POSITIVE));
		
		// stalactite
		modelService.AddStalactiteDefinition(new StalactiteDefinition(24, 30, 3, 3, 0.6f, STALACTITE_GROWTH_DIR.DOWN, VoxelType.STALACTITE));
		modelService.AddStalactiteDefinition(new StalactiteDefinition(30, 40, 4, 3, 0.6f, STALACTITE_GROWTH_DIR.DOWN, VoxelType.STALACTITE));
		modelService.AddStalactiteDefinition(new StalactiteDefinition(14, 20, 2, 3, 0.6f, STALACTITE_GROWTH_DIR.DOWN, VoxelType.STALACTITE));
		modelService.AddStalactiteDefinition(new StalactiteDefinition(14, 20, 3, 3, 0.6f, STALACTITE_GROWTH_DIR.DOWN, VoxelType.STALACTITE));
		modelService.AddStalactiteDefinition(new StalactiteDefinition(22, 30, 3, 3, 0.6f, STALACTITE_GROWTH_DIR.UP, VoxelType.STALACTITE));
		modelService.AddStalactiteDefinition(new StalactiteDefinition(14, 20, 2, 3, 0.6f, STALACTITE_GROWTH_DIR.UP, VoxelType.STALACTITE));
    }
}
