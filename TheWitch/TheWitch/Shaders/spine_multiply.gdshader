shader_type canvas_item;
// Multiply blend for Spine slots (SpineSprite.multiply_material).
// The stock multiply material ignores alpha, so every transparent texel of the slot's mesh
// multiplies the scene by its (black) rgb and the whole mesh draws as a black shape. Fading
// the colour towards white by alpha makes transparent texels a no-op, like Photoshop/Spine.
render_mode blend_mul, unshaded;

void fragment() {
	// COLOR arrives as slot colour * atlas texel (straight alpha; the atlas is not premultiplied).
	vec4 c = COLOR;
	COLOR = vec4(mix(vec3(1.0), c.rgb, c.a), 1.0);
}
