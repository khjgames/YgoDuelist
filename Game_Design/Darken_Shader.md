The Darken blend mode works by comparing the base and blend layers and keeping the darker of the two values for each color channel. 

```C#
shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
    vec4 bg_color = texture(screen_texture, SCREEN_UV);
    vec4 tex_color = texture(TEXTURE, UV);
    // Darken blend: take the minimum of each channel
    COLOR.rgb = min(bg_color.rgb, tex_color.rgb);
    COLOR.a = tex_color.a;
}
```

Formula: min(base, blend).

Implementation: 

Assign this shader to a ShaderMaterial on your Sprite2D or ColorRect. 

Note that for hint_screen_texture to work correctly between overlapping sprites, you may need a BackBufferCopy node to refresh the screen buffer. 
