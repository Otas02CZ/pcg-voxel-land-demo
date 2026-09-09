// FILE: SkyShader.gdshader
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Shader for the world sky. Moving procedural noise clouds dependent on current
// weather intensity (color, density). Includes basic day-night cycle with star display at night.

shader_type sky;

// general parameters
uniform sampler2D sky_background_gradient;
uniform float weather_intensity = 0.5;
uniform float time_of_day = 1.0;
uniform sampler2D lighting_gradient;
uniform sampler2D star_mask_gradient;
uniform vec4 clear_color : source_color = vec4(0,0,0,0);

// cloud related parameters
group_uniforms clouds_base;
uniform sampler3D cloud_noise;
uniform vec2 cloud_speed = vec2(0.001, 0.01);
uniform float cloud_change_speed = 0.05;
uniform float cloud_scale = 100;
uniform float cloud_render_dist_max = 10000;
uniform sampler2D cloud_gradient_base;
uniform sampler2D cloud_gradient_alt;
// cloud layer parameters
group_uniforms clouds_layer_1;
uniform float cloud_height_1 = 200;
uniform float cloud_noise_threshold_1_min = 0.2;
uniform float cloud_noise_threshold_1_max = 0.8;
uniform float cloud_softness_1_low = 0.2;
uniform float cloud_softness_1_high = 0.2;
group_uniforms clouds_layer_2;
uniform float cloud_height_2 = 300;
uniform float cloud_noise_threshold_2_min = 0.2;
uniform float cloud_noise_threshold_2_max = 0.8;
uniform float cloud_softness_2_low = 0.2;
uniform float cloud_softness_2_high = 0.2;
group_uniforms clouds_layer_3;
uniform float cloud_height_3 = 3000;
uniform float cloud_noise_threshold_3_min = 0.2;
uniform float cloud_noise_threshold_3_max = 0.8;
uniform float cloud_softness_3_low = 0.2;
uniform float cloud_softness_3_high = 0.2;
// night sky stars
group_uniforms stars;
uniform vec4 star_color : source_color;
uniform sampler2D star_noise;
uniform float star_noise_threshold_max = 0.2;
uniform float star_height = 800;
uniform vec2 star_speed = vec2(0.001, 0.01);
uniform float star_scale = 0.001;

/*
* Calculates intersection with a plane at given height from given point.
*/
float plane_intersection(vec3 origin, vec3 direction, float height) {
	return (height - origin.y) / direction.y;
}

/*
* Calculates exact coordinate for obtaining cloud noise values.
*/
vec3 cloud_layer_position(vec3 origin, vec3 direction, float t) {
	vec3 cloud_position = origin + direction * t;
	vec2 cloud_uv = cloud_position.xz * cloud_scale + TIME * cloud_speed;
	float cloud_y = TIME * cloud_change_speed;
	return vec3(cloud_uv.x, cloud_y, cloud_uv.y);
}

/*
* Calculates exact coordinate for obtaining star noise values.
*/
vec2 star_layer_position(vec3 origin, vec3 direction, float t) {
	vec3 star_position = origin + direction * t;
	vec2 star_uv = star_position.xz * star_scale + TIME * star_speed;
	return star_uv;
}

/*
* Calculates color for given cloud layer
*/
vec4 cloud_layer_color(vec3 cloud_xyz, float threshold_min, float threshold_max, float cloud_softness_low, float cloud_softness_high, vec4 light_modulation) {
	float noise = texture(cloud_noise, cloud_xyz).r;
	float cloud_threshold = mix(threshold_min, threshold_max, 1.0 - weather_intensity);
	
	float cloud_base = smoothstep(cloud_threshold - cloud_softness_low, cloud_threshold + cloud_softness_high, noise);
	
	vec4 cloud_color_1 = texture(cloud_gradient_base, vec2(weather_intensity, 0));
	vec4 cloud_color_2 = texture(cloud_gradient_alt, vec2(weather_intensity, 0));
	
	vec4 color = mix(cloud_color_1, cloud_color_2, cloud_base);
	color *= light_modulation;
	vec4 final_color = mix(clear_color, color, cloud_base);
	return vec4(final_color.rgb, cloud_base);
}

/*
* Assembles star layer color at given uv.
*/
vec4 star_layer_color(vec2 star_uv) {
	float star = texture(star_noise, star_uv).r;
	
	vec4 color = clear_color;
	if (star <= star_noise_threshold_max) {
		color = star_color;
	}
	
	float star_visibility = texture(star_mask_gradient, vec2(time_of_day, 0)).r;
	return vec4(color.rgb, star_visibility);
}

void sky() {
	// Called for every visible pixel in the sky background, as well as all pixels
	// in the radiance cubemap.
	
	// calculate intersections for all cloud heights and the star layer
	float t1 = plane_intersection(POSITION, EYEDIR, cloud_height_1);
	float t2 = plane_intersection(POSITION, EYEDIR, cloud_height_2);
	float t3 = plane_intersection(POSITION, EYEDIR, cloud_height_3);
	float sky_t = plane_intersection(POSITION, EYEDIR, star_height);
	// sample day-night light adjusting gradient
	vec4 lighting_adj = texture(lighting_gradient, vec2(time_of_day, 0));
	if (t1 <= 0.0 || t1 >= cloud_render_dist_max) { // limits
		COLOR = texture(sky_background_gradient, vec2(time_of_day, 0)).rgb * lighting_adj.rgb; // only general color
	} else { // clouds
		// exact positions
		vec3 cloud_xyz_1 = cloud_layer_position(POSITION, EYEDIR, t1);
		vec3 cloud_xyz_2 = cloud_layer_position(POSITION, EYEDIR, t2);
		vec3 cloud_xyz_3 = cloud_layer_position(POSITION, EYEDIR, t3);
		vec2 star_uv = star_layer_position(POSITION, EYEDIR, sky_t);
		// layer colors
		vec4 cloud_color_final_1 = cloud_layer_color(cloud_xyz_1, cloud_noise_threshold_1_min, cloud_noise_threshold_1_max, cloud_softness_1_low, cloud_softness_1_high, lighting_adj);
		vec4 cloud_color_final_2 = cloud_layer_color(cloud_xyz_2, cloud_noise_threshold_2_min, cloud_noise_threshold_2_max, cloud_softness_2_low, cloud_softness_2_high, lighting_adj);
		vec4 cloud_color_final_3 = cloud_layer_color(cloud_xyz_3, cloud_noise_threshold_3_min, cloud_noise_threshold_3_max, cloud_softness_3_low, cloud_softness_3_high, lighting_adj);
		vec4 star_color_final = star_layer_color(star_uv);
		// color combination (from back to front -> sky background -> distant cloud layers, ... -> closest cloud layers)
		vec3 final_cloud_color = texture(sky_background_gradient, vec2(time_of_day, 0)).rgb;
		final_cloud_color = mix(final_cloud_color, star_color_final.rgb, star_color_final.a);
		final_cloud_color = mix(final_cloud_color, cloud_color_final_3.rgb, cloud_color_final_3.a);
		final_cloud_color = mix(final_cloud_color, cloud_color_final_2.rgb, cloud_color_final_2.a);
		final_cloud_color = mix(final_cloud_color, cloud_color_final_1.rgb, cloud_color_final_1.a);
		
		COLOR = vec3(final_cloud_color);
	}
}
