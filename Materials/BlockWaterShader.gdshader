// FILE: BlockWaterShader.gdshader
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Shader for water surfaces. Simulates waves by vertex transformations and moving variance effect of the voxel fragments.

shader_type spatial;

render_mode cull_back;

uniform vec3 water_color : source_color;

uniform sampler3D variation_noise;

uniform float voxels_per_meter = 16.0;
uniform float variation_strength = 1.0;
uniform float variation_speed = 4;

uniform float noise_scale = 0.05;

uniform float wave_height = 0.05;
uniform float wave_speed = 1.5;
uniform float wave_scale = 0.8;

uniform float y_offset = 0.0625;

uniform float water_alpha = 0.8;

varying vec3 world_position;

/*
* Vertex shader. Uses precomputed noise texture to simulate water waves effect by offsetting
* vertical positions of vertices.
*/
void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	
	vec3 wave_position = world_position * 0.25;
	wave_position.xz += TIME * vec2(0.3, 0.25);
	
	float wave = texture(variation_noise, wave_position).r;
	wave = (wave - 0.5) * 2.0;
	
	VERTEX.y += wave * wave_height - y_offset;
}

/*
* Fragment shader. Attempts a moving per-voxel variance effect. Reconstructs voxel coordinates
* and uses two precomputed noise textures to change the default color of water voxel fragments.
*/
void fragment()
{
	vec3 color = water_color;
	
	vec3 voxel = floor(world_position * voxels_per_meter);
	vec3 local = fract(world_position * voxels_per_meter);
	
	// movement
	vec3 flow = vec3(TIME * 0.03, 0.0, TIME * 0.025) * variation_speed;
	
	// break down noise pattern by mixing two noise samples
	
	// normal noise sample
	float block_noise = texture(
	    variation_noise,
	    voxel * noise_scale + flow
	).r;
	
	// detail noise sample
	float detail_noise = texture(
	    variation_noise,
	    (voxel + local * 0.5) * noise_scale * 3.0 + flow * 1.5
	).r;
	
	float rand = mix(block_noise, detail_noise, 0.35);
	
	float variation = (rand - 0.5) * variation_strength;
	
	color *= (1.0 + variation);
	
	// color correction
	color = pow(color, vec3(2.2));
	
	ALBEDO = color;
	ROUGHNESS = 0.08;
	METALLIC = 0.0;
	ALPHA = water_alpha;
}
