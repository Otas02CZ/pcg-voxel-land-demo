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
        const float maxLeafRadius = 0.4f;
		const float leafRadiusScale = 3.4f;

		const float maxLeafRadiusWeak = 0.38f;
		const float leafRadiusScaleWeak = 0.8f;
		
		// Trees deciduous
		// SMALL HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 90, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 15+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 300, 75, 20f, 0.4f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 5+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 280, 100, 50f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 13+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.HEALTHY, 300, 80, 40f, 0.25f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 11+8, EnvelopeType.OVAL, 16f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// SMALL WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 240, 70, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 240, 75, 20f, 0.8f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 0+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 240, 80, 50f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 8+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.WEAK, 180, 60, 40f, 0.7f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 6+8, EnvelopeType.OVAL, 16f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// SMALL DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 190, 40, 80f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 12+8, EnvelopeType.SPHERE, 20f, 40f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 240, 40, 35f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 6+8, EnvelopeType.PARABOLOID_CUP, 20f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 240, 40, 50f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CYLINDER, 16f, 50f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.SMALL, TreeState.DEAD, 180, 45, 40f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 8+8, EnvelopeType.OVAL, 16f, 45f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		
		// MEDIUM HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 650, 120, 40f, 0.25f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 520, 200, 60f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 500, 250, 90f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 200, 50, 30f, 0.50f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 38, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 500, 160, 50f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CYLINDER, 24f, 100f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.HEALTHY, 600, 200, 60f, 0.50f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// MEDIUM WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 650, 120, 40f, 0.25f, 0.75f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 520, 200, 60f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 500, 250, 90f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 200, 50, 30f, 0.50f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 3+8, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 500, 160, 50f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CYLINDER, 24f, 100f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.WEAK, 600, 200, 60f, 0.50f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 16+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		
		// MEDIUM DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 500, 70, 40f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 14+8, EnvelopeType.OVAL, 20f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 450, 80, 60f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 14+8, EnvelopeType.OVAL, 30f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 400, 90, 90f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 14+8, EnvelopeType.SPHERE, 40f, 80f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 200, 40, 30f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 6+8, EnvelopeType.OVAL, 25f, 50f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 500, 90, 50f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 14+8, EnvelopeType.CYLINDER, 24f, 100f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.MEDIUM, TreeState.DEAD, 400, 65, 60f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 19+8, EnvelopeType.OVAL, 35f, 65f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		
		// LARGE HEALTHY
		
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1350, 260, 45f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 15+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1200, 240, 40f, 1f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 3+8, EnvelopeType.OVAL, 35f, 150f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.HEALTHY, 1200, 240, 50f, 1f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CYLINDER, 50f, 110f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// LARGE WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1200, 220, 45f, 2f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 15+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1100, 210, 40f, 2f, 0.50f, new Vector3(-0.2f, 0.2f, -0.2f), 3+8, EnvelopeType.OVAL, 35f, 150f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.WEAK, 1100, 210, 50f, 2f, 0.5f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CYLINDER, 50f, 110f, 
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// LARGE DEAD
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.DECIDUOUS, TreeSize.LARGE, TreeState.DEAD, 600, 90, 45f, 2f, 1.2f, new Vector3(0.2f, 0.2f, -0.2f), 24+8, EnvelopeType.SPHERE, 55f, 110f,
			trunk: VoxelType.WOOD, leaves: VoxelType.DECIDUOUS_LEAVES, maxLeafRadius: 0, leafRadiusScale: 0, offsetY:-8));
		
		
		// Trees coniferous
		// LARGE HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1700, 280, 6f, 0.7f, 0.50f, new Vector3(0.4f, -0.2f, -0.3f), 60+8, EnvelopeType.CONIFER_WEDGE, 40f, 200f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 1000, 225, 6f, 0.9f, 0.75f, new Vector3(0.2f, 0.2f, 0.4f), 15+8, EnvelopeType.CONIFER_WEDGE, 40f, 200f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.HEALTHY, 900, 195, 75f, 2.1f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CONE, 30f, 180f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// LARGE WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 1700, 225, 6f, 0.7f, 0.50f, new Vector3(0.4f, -0.2f, -0.3f), 60+8, EnvelopeType.CONIFER_WEDGE, 40f, 200f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 1000, 225, 6f, 0.9f, 0.75f, new Vector3(0.2f, 0.2f, 0.4f), 15+8, EnvelopeType.CONIFER_WEDGE, 40f, 200f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.LARGE, TreeState.WEAK, 800, 180, 70f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 25+8, EnvelopeType.CONE, 30f, 180f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));	
		// MEDIUM HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 1200, 130, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 35+8, EnvelopeType.CONIFER_WEDGE, 30, 90f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 800, 130, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 25, 90f, kTop:-0.5f, kBottom:-0.5f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.HEALTHY, 450, 120, 70f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CONE, 25f, 100f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		
		// MEDIUM WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 800, 120, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 35+8, EnvelopeType.CONIFER_WEDGE, 30, 90f, kTop:0.8f, kBottom:0.8f,
		   	trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 800, 120, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 25, 90f, kTop:-0.5f, kBottom:-0.5f,
		   	trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.MEDIUM, TreeState.WEAK, 500, 110, 70f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 10+8, EnvelopeType.CONE, 25f, 100f,
		   	trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		// SMALL HEALTHY
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 260, 90, 25f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 8+8, EnvelopeType.CONE, 20f, 60f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: leafRadiusScale, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.HEALTHY, 300, 70, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 15f, 65f, kTop:0.8f, kBottom:0.8f,
		   trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadius, leafRadiusScale: 2f, offsetY:-8));
		
		// SMALL WEAK
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 260, 100, 25f, 2f, 1f, new Vector3(-0.2f, 0.2f, -0.2f), 8+8, EnvelopeType.CONE, 20f, 60f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		modelService.AddTreeDefinition(new TreeDefinition(TreeType.CONIFER, TreeSize.SMALL, TreeState.WEAK, 300, 70, 8f, 1f, 0.75f, new Vector3(0.4f, -0.2f, -0.3f), 25+8, EnvelopeType.CONIFER_WEDGE, 15f, 65f, kTop:0.8f, kBottom:0.8f,
			trunk: VoxelType.WOOD, leaves: VoxelType.CONIFEROUS_LEAVES, maxLeafRadius: maxLeafRadiusWeak, leafRadiusScale: leafRadiusScaleWeak, offsetY:-8));
		
		
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
		modelService.AddGrassDefinition(new GrassDefinition(2, 9, 3, 10, 4, 4, VoxelType.PLANT));
		modelService.AddGrassDefinition(new GrassDefinition(3, 10, 4, 12, 4, 4, VoxelType.PLANT));
		modelService.AddGrassDefinition(new GrassDefinition(4, 12, 2, 8, 4, 4, VoxelType.PLANT));
		modelService.AddGrassDefinition(new GrassDefinition(2, 9, 4, 12, 4, 4, VoxelType.PLANT));
		modelService.AddGrassDefinition(new GrassDefinition(3, 10, 3, 10, 4, 4, VoxelType.PLANT));
		modelService.AddGrassDefinition(new GrassDefinition(4, 12, 2, 8, 4, 4, VoxelType.PLANT));
		
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
		modelService.AddPlantBushDefinition(new PlantBushDefinition());
		modelService.AddPlantBushDefinition(new PlantBushDefinition());
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.WHITE));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.WHITE));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.BLUE));
		modelService.AddPlantBushDefinition(new PlantBushDefinition(flowerVoxelType:VoxelType.BLUE));
		
		// rocks
		modelService.AddRockDefinition(new RockDefinition(50, 50, 50, 25));
		modelService.AddRockDefinition(new RockDefinition(30, 30, 30, 15));
		modelService.AddRockDefinition(new RockDefinition(20, 20, 20, 10));
		modelService.AddRockDefinition(new RockDefinition(16, 16, 16, 8));
		
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
