shader_type spatial;
varying vec3 world_position;
void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}
void fragment() {
	float stripe = step(0.5, fract((world_position.x + world_position.z) * 2.2));
	ALBEDO = mix(vec3(0.045, 0.06, 0.075), vec3(0.72, 0.49, 0.14), stripe);
	ROUGHNESS = 0.8;
}
