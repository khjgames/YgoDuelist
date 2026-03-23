shader_type canvas_item;

/// Grayscale mask: white = show portrait, black = hide (same pixel size as portrait art).
uniform sampler2D mask_tex : filter_linear, repeat_disable;

void fragment() {
	vec4 art = texture(TEXTURE, UV);
	vec4 ms = texture(mask_tex, UV);
	float m = max(ms.r, max(ms.g, ms.b));
	COLOR = vec4(art.rgb, art.a * m);
}
