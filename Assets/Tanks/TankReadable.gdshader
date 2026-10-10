shader_type spatial;
render_mode diffuse_burley, specular_disabled;

uniform sampler2D base_color : source_color, filter_linear_mipmap, repeat_enable;
uniform float scene_light_weight : hint_range(0.0, 1.0) = 0.22;
uniform float armor_palette_weight : hint_range(0.0, 1.0) = 0.0;
uniform vec4 armor_color : source_color = vec4(0.40, 0.18, 0.53, 1.0);
varying vec3 world_normal;

void vertex() {
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	vec3 color = pow(texture(base_color, UV).rgb, vec3(0.85));
	// Optional team-armor palette keeps the original texture's panel shading.
	float value = dot(color, vec3(0.2126, 0.7152, 0.0722));
	float armor_mask = smoothstep(0.22, 0.46, value);
	vec3 armored = mix(vec3(0.023, 0.027, 0.033), armor_color.rgb, armor_mask);
	armored *= 0.55 + value * 0.9;
	color = mix(color, armored, armor_palette_weight);
	// Neutral fill preserves team colors in purple shadows; the normal-based
	// gradient retains turret and track volume even without direct scene light.
	float shape = 0.80 + 0.20 * max(dot(normalize(world_normal), normalize(vec3(-0.4, 0.85, 0.3))), 0.0);
	ALBEDO = color * scene_light_weight;
	EMISSION = color * (1.0 - scene_light_weight) * shape * 1.1;
	ROUGHNESS = 1.0;
}
