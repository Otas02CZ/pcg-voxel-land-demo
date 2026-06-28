// FILE: BlockShader.gdshader
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Shader for solid surfaces. Attempts a per-voxel block variance effect.
// DISCLAIMER: This file uses random hash function taken from https://www.shadertoy.com/view/4djSRW

shader_type spatial;

render_mode cull_back, depth_draw_opaque;

uniform sampler2D atlas_texture;

uniform float voxels_per_meter = 16.0;
uniform float variation_strength = 0.22;

varying vec3 world_position;

/*
* Returns deterministic hash random number for supplied 3D coordinate.
* Taken from https://www.shadertoy.com/view/4djSRW
*/
float random(vec3 position)
{
	position = fract(position * .1031);
	position += dot(position, position.zyx + 33.33);
	return fract((position.x + position.y) * position.z);
}

/*
* Vertex shader, only pass the position
*/
void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

/*
* Fragment shader, reconstructs per-voxel 3D coordinates and uses it
* to obtain a deterministic position based random value, which varies the
* default voxel type color of the fragment.
*/
void fragment()
{
	vec3 color = texture(atlas_texture, UV).rgb;
	
	vec3 voxel = floor(world_position * voxels_per_meter);
	
	float rand = random(voxel);
	
	float variation = (rand - 0.5) * variation_strength;
	
	color = color * (1.0 + variation);
	color = pow(color, vec3(2.2));
	ALBEDO = color;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
}
