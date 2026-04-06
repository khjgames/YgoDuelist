shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
	vec4 bg_color = texture(screen_texture, SCREEN_UV);
	vec4 tex_color = texture(TEXTURE, UV);
	COLOR.rgb = min(bg_color.rgb, tex_color.rgb);
	COLOR.a = tex_color.a;
}
