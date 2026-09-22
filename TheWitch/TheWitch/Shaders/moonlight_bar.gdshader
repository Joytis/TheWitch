shader_type canvas_item;

// Moonlight HP-bar overlay — a night-sky take on the base game's doom_bar.gdshader.
// Same bones as Doom: two octaves of noise in SCREEN_UV (nine-patch UVs are per-patch and useless),
// hard-stepped into a 3-stop gradient so it reads as slow cel-shaded clouds. On top: sparse tiny stars.
// Every knob Doom hard-codes is a uniform here; Doom's own values are noted next to each default.
// COLOR.a is left alone so the nine-patch's clip/modulate alpha still applies.

uniform sampler2D noise_tex : repeat_enable;
uniform sampler2D gradient_tex;

group_uniforms clouds;
uniform float cloud_scale : hint_range(0.1, 4.0) = 1.0;            // noise UV multiplier (Doom = 1.0)
uniform float cloud_speed : hint_range(0.0, 0.005) = 0.0005;       // drift, UV/s (Doom = 0.0005)
uniform float cloud_warp : hint_range(0.0, 1.0) = 0.25;            // domain-warp strength; the "water" look (Doom = 1.0)
uniform float cloud_warp_scale : hint_range(0.01, 1.0) = 0.1;      // UV scale of the warp field (Doom = 0.1)
uniform float cloud_edge : hint_range(0.0, 1.0) = 0.5;             // cel threshold centre (Doom = 0.5)
uniform float cloud_softness : hint_range(0.0, 0.5) = 0.1;         // half-width of the step (Doom = 0.1)
group_uniforms;

group_uniforms sky;
uniform vec4 sky_color : source_color = vec4(0.14, 0.17, 0.46, 1.0); // flat night-sky base
uniform float cloud_mix : hint_range(0.0, 1.0) = 0.6;              // 0 = flat sky_color, 1 = full cloud gradient
group_uniforms;

group_uniforms rim;
uniform vec4 rim_color : source_color = vec4(0.78, 0.88, 1.0, 1.0);  // moonlit edge glow
uniform float rim_intensity : hint_range(0.0, 2.0) = 0.6;          // 0 = off (additive)
uniform float rim_width : hint_range(0.01, 0.4) = 0.2;             // glow reach from the edge, UV (of the fill texture)
uniform float rim_falloff : hint_range(0.5, 4.0) = 1.5;            // curve exponent: >1 hugs the edge, <1 spreads inward
uniform float rim_inset : hint_range(0.0, 0.5) = 0.22;            // top/bottom UV inset; fill overhangs the clip mask (4/18 px)
group_uniforms;

group_uniforms stars;
uniform float star_cell_px : hint_range(4.0, 32.0) = 9.0;          // one star candidate per NxN screen px
uniform float star_chance : hint_range(0.0, 1.0) = 0.10;           // fraction of cells that hold a star
uniform float star_size_px : hint_range(0.5, 3.0) = 1.1;           // star radius, px
uniform float star_brightness : hint_range(0.0, 2.0) = 0.9;
uniform float star_twinkle_speed : hint_range(0.0, 4.0) = 0.8;
uniform float star_twinkle_depth : hint_range(0.0, 1.0) = 0.45;    // 0 = steady, 1 = fully off at the trough
uniform float star_cloud_dim : hint_range(0.0, 1.0) = 0.6;         // how much the bright cloud band hides stars
group_uniforms;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void fragment() {
    // --- clouds: Doom's two-octave domain-warped noise, with the warp made dial-able ---
    vec2 uv = SCREEN_UV * cloud_scale;
    float warp1 = texture(noise_tex, uv * cloud_warp_scale + TIME * cloud_speed + 0.24).r * cloud_warp;
    float col1 = texture(noise_tex, uv + TIME * cloud_speed + warp1).r;

    float warp2 = texture(noise_tex, uv * cloud_warp_scale + TIME * cloud_speed + 0.24).r * cloud_warp;
    float col2 = texture(noise_tex, uv + TIME * -cloud_speed * 1.4 + warp2).r;

    float cloud = col1 * 0.75 + col2 * 0.35;
    cloud = smoothstep(cloud_edge - cloud_softness, cloud_edge + cloud_softness, cloud);

    vec3 cloud_col = texture(gradient_tex, vec2(cloud, 0.0)).rgb;
    vec3 col = mix(sky_color.rgb, cloud_col, cloud_mix);

    // --- rim: additive moonlit glow along the slice's edge, all in fill-texture UV ---
    // Vertically UV.y spans the whole node (no top/bottom patches); it's inset because the fill overhangs
    // the Mask's clip, so the visible band is UV.y in [rim_inset, 1 - rim_inset].
    // Horizontally the nine-patch restarts UV per patch: the 1:1 end caps run 0..0.4 / 0.6..1, and the
    // stretched middle sits at 0.4..0.6 — always >= 0.4 from an edge, so it never picks up rim.
    float d_left = UV.x;
    float d_right = 1.0 - UV.x;
    float d_top = UV.y - rim_inset;
    float d_bottom = 1.0 - rim_inset - UV.y;
    float d_edge = max(min(min(d_left, d_right), min(d_top, d_bottom)), 0.0);
    float rim = pow(1.0 - smoothstep(0.0, rim_width, d_edge), rim_falloff);
    col += rim_color.rgb * rim * rim_intensity;

    // --- stars: sparse hashed points in screen-pixel space, slow independent twinkle ---
    vec2 px = SCREEN_UV / SCREEN_PIXEL_SIZE;
    vec2 cell_uv = px / star_cell_px;
    vec2 cell = floor(cell_uv);
    float h = hash21(cell);
    if (h > 1.0 - star_chance) {
        // jitter the star inside its cell so they don't sit on a grid
        vec2 jitter = vec2(hash21(cell + 7.0), hash21(cell + 13.0)) * 0.6 + 0.2;
        vec2 d = (fract(cell_uv) - jitter) * star_cell_px; // px from star centre
        float star = 1.0 - smoothstep(0.0, star_size_px, length(d));

        float phase = hash21(cell + 29.0) * 6.2831;
        float rate = star_twinkle_speed * (0.7 + hash21(cell + 41.0) * 0.6);
        float tw = 1.0 - star_twinkle_depth * (0.5 + 0.5 * sin(TIME * rate + phase));

        // stars show through the dark sky, dim behind the bright cloud band
        float sky = 1.0 - cloud * cloud_mix * star_cloud_dim;
        col += vec3(0.95, 0.97, 1.0) * star * tw * star_brightness * sky;
    }

    COLOR.rgb = col;
}
