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
uniform float cloud_sphere_radius = 50000;

// sun and moon
group_uniforms sun_and_moon;
uniform float sun_size_threshold = 0.995;
uniform float moon_size_threshold = 0.9975;

// cloud related parameters
group_uniforms clouds_base;
uniform sampler3D cloud_noise;
uniform vec2 cloud_speed = vec2(0.001, 0.01);
uniform float cloud_change_speed = 0.05;
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
* Calculates ray intersection with a sphere.
* Used for mapping the feature layers on a spherical dome.
* Assembled with help from: https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection.html
*/
float ray_sphere_intersection(vec3 origin, vec3 direction, float cloud_height) {
	vec3 center = vec3(0.0, cloud_height - cloud_sphere_radius, 0.0);
	vec3 origin_center = origin - center;
	// analytic solution
	// a = 1
	float b = 2.0 * dot(direction, origin_center);
	float c = dot(origin_center, origin_center) - cloud_sphere_radius * cloud_sphere_radius;
	float discriminant = b * b - 4.0 * c; // d = bb - 4ac
	if (discriminant <= 0.0) {
		return 0.0;
	}
	
	float discriminant_sqrt = sqrt(discriminant);
	
	float t0 = -b + discriminant_sqrt;
	float t1 = -b - discriminant_sqrt;
	// positive intersection
	return t0 > 0.0 ? t0 : t1;
}

/*
* Calculates exact coordinate for obtaining cloud noise values.
*/
vec3 cloud_layer_position(vec3 origin, vec3 direction, float t) {
	vec3 cloud_position = origin + direction * t;
	vec2 cloud_uv = cloud_position.xz * 0.00035 + TIME * cloud_speed;
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

/*
* Assembles sun and moon layer color depending on current light and camera directions.
*/
vec4 sun_moon_layer_color(vec3 direction) {
	vec4 color = vec4(0.0);
	if (LIGHT0_ENABLED) { // sun
		float sun_dot = dot(direction, LIGHT0_DIRECTION);
		float sun_disc = smoothstep(sun_size_threshold, 1, sun_dot);
		color += vec4(LIGHT0_COLOR * LIGHT0_ENERGY, sun_disc * LIGHT0_ENERGY);
	}
	
	if (LIGHT1_ENABLED) { // moon
		float moon_dot = dot(direction, LIGHT1_DIRECTION);
		float moon_disc = smoothstep(moon_size_threshold, 1, moon_dot);
		color += vec4(LIGHT1_COLOR * LIGHT1_ENERGY, moon_disc * LIGHT1_ENERGY);
	}
	
	return color;
}

void sky() {
	// Called for every visible pixel in the sky background, as well as all pixels
	// in the radiance cubemap.
	
	// calculate dome intersections for all cloud heights and the star layer
	float t1 = ray_sphere_intersection(POSITION, EYEDIR, cloud_height_1);
	float t2 = ray_sphere_intersection(POSITION, EYEDIR, cloud_height_2);
	float t3 = ray_sphere_intersection(POSITION, EYEDIR, cloud_height_3);
	float sky_t = ray_sphere_intersection(POSITION, EYEDIR, star_height);
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
		vec4 sun_color_final = sun_moon_layer_color(EYEDIR);
		// color combination from back to front:
		// from sky background - stars - sun and moon - distant cloud layers, ... to closest cloud layers
		vec3 final_cloud_color = texture(sky_background_gradient, vec2(time_of_day, 0)).rgb;
		final_cloud_color = mix(final_cloud_color, star_color_final.rgb, star_color_final.a);
		final_cloud_color = mix(final_cloud_color, sun_color_final.rgb, sun_color_final.a);
		final_cloud_color = mix(final_cloud_color, cloud_color_final_3.rgb, cloud_color_final_3.a); // distant
		final_cloud_color = mix(final_cloud_color, cloud_color_final_2.rgb, cloud_color_final_2.a);
		final_cloud_color = mix(final_cloud_color, cloud_color_final_1.rgb, cloud_color_final_1.a); // close
		
		COLOR = vec3(final_cloud_color);
	}
}
