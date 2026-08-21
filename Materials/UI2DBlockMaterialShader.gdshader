// FILE: UI2DBlockMaterial.gdshader
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Canvas Item shader for display of voxel types, attempts to create voxel variance effect.
// DISCLAIMER: This file uses random hash function taken from https://www.shadertoy.com/view/4djSRW

shader_type canvas_item;

// float precission fix for voxel coords reconstruction
const float EPSILON = 0.001;

uniform sampler2D atlas_texture;

uniform vec2 atlas_uv = vec2(0.0); // uv in the texture atlas, regular uv used to imitate position

uniform float resolution = 16.0; // voxel grid res per meter
uniform float variation_strength = 0.22;

/*
* Returns deterministic hash random number for supplied 2D coordinate.
* Taken from https://www.shadertoy.com/view/4djSRW
*/
float random(vec2 position)
{
	vec3 position3 = fract(vec3(position.xyx) * .1031);
	position3 += dot(position3, position3.yzx + 33.33);
	return fract((position3.x + position3.y) * position3.z);
}

/*
* Fragment shader, uses uv of the canvas item to imitate per-voxel coords in
* 2D space and create 2D voxel-variance effect by varying the fragment color with
* position based deterministic random value.
*/
void fragment()
{
	vec3 color = texture(atlas_texture, atlas_uv).rgb;
	
	vec2 voxel = floor(UV * resolution + EPSILON); // imitate voxel grid in 2D space with fragment uv
	float rand = random(voxel);
	float variation = (rand - 0.5) * variation_strength;
	
	color = color * (1.0 + variation);
	COLOR = vec4(color, 1.0);
}
