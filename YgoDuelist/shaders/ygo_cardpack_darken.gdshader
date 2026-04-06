shader_type canvas_item;

// Skip when mask coverage is ~0 (shape / soft edges from the tint PNG).
uniform float alpha_threshold : hint_range(0.0, 1.0, 0.001) = 0.00392156862745098;

// Max tint strength on top of the pack art (142/255); mask white × full alpha no longer hits 100%.
uniform float layer_opacity : hint_range(0.0, 1.0, 0.001) = 0.5568627450980392;

uniform sampler2D background_texture : repeat_disable, filter_linear_mipmap;
uniform sampler2D mask_texture : repeat_disable, filter_linear_mipmap;
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec4 bg_color = texture(background_texture, UV);
	vec4 over = texture(mask_texture, UV);
	float lum = max(over.r, max(over.g, over.b));
	float mask_amount = clamp(lum * over.a, 0.0, 1.0);
	if (mask_amount <= alpha_threshold) {
		COLOR = vec4(0.0);
	} else {
		float blend = mask_amount * layer_opacity;
		vec3 darkened_tint = min(bg_color.rgb, tint_color.rgb);
		COLOR.rgb = mix(bg_color.rgb, darkened_tint, blend);
		COLOR.a = 1.0;
	}
}
