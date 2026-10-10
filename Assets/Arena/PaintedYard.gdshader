shader_type spatial;
render_mode diffuse_toon;
varying vec3 world_position;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}
float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1,0)), f.x),
		mix(hash(i + vec2(0,1)), hash(i + vec2(1,1)), f.x), f.y);
}
void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}
void fragment() {
	vec2 p = world_position.xz;
	float patch = smoothstep(0.51, 0.54, noise(p * 0.43));
	vec3 paint = mix(vec3(0.30, 0.21, 0.23), vec3(0.39, 0.29, 0.26), patch * 0.12);
	float edge = smoothstep(8.8, 9.9, abs(p.x)) + smoothstep(5.9, 6.9, abs(p.y));
	paint = mix(paint, vec3(0.44, 0.30, 0.25), clamp(edge, 0.0, 1.0) * noise(p * 1.7) * 0.5);
	ALBEDO = paint;
	ROUGHNESS = 1.0;
}
