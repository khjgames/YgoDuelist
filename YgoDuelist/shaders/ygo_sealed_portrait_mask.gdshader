shader_type canvas_item;

// Black in mask = hidden, white = visible (luminance × alpha).
uniform sampler2D mask_texture : repeat_disable, filter_linear;

void fragment() {
	vec4 art = texture(TEXTURE, UV);
	vec4 mk = texture(mask_texture, UV);
	float lum = max(mk.r, max(mk.g, mk.b));
	float a = clamp(lum * mk.a, 0.0, 1.0);
	COLOR.rgb = art.rgb;
	COLOR.a = art.a * a;
}
