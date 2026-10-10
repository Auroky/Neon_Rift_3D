shader_type spatial;
render_mode diffuse_burley;

varying vec3 world_position;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 p = world_position.xz;
	vec2 tile = floor((p + vec2(10.0, 7.0)) / 2.0);
	vec2 cell = mod(p + vec2(10.0, 7.0), 2.0);
	vec2 edge = min(cell, 2.0 - cell);
	float aa = max(length(fwidth(p)), 0.008);
	float seam = 1.0 - smoothstep(0.012, 0.012 + aa, min(edge.x, edge.y));
	vec3 concrete = vec3(0.23, 0.29, 0.34);
	concrete *= 0.94 + hash(tile) * 0.12;
	// Low contrast, broad wear stays quiet behind tanks and projectiles.
	concrete *= 0.992 + 0.008 * sin(p.x * 5.1 + sin(p.y * 2.4));
	concrete = mix(concrete, vec3(0.115, 0.15, 0.18), seam * 0.4);
	float radius = length(p);
	float ring = 1.0 - smoothstep(0.022, 0.022 + aa, abs(radius - 2.35));
	float dash = step(0.30, fract(atan(p.y, p.x) * 12.0 / 6.283185));
	concrete = mix(concrete, vec3(0.38, 0.44, 0.46), ring * dash * 0.42);
	ALBEDO = concrete;
	ROUGHNESS = 0.93;
}
