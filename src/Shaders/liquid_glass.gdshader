shader_type canvas_item;

// Liquid-glass pane: blurs and bends whatever is drawn behind it, with chromatic edges and a lit rim.

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear_mipmap;

uniform vec2 rect_size = vec2(300.0, 400.0);   // canvas units
uniform float ui_scale = 1.0;                  // screen pixels per canvas unit
uniform float radius = 32.0;
uniform float blur_lod : hint_range(0.0, 7.0) = 2.6;
uniform float refraction = 16.0;               // px of lens bend at the rim
uniform float bevel = 22.0;                    // px the lens effect reaches inward
uniform float dispersion : hint_range(0.0, 1.0) = 0.35;
uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 0.09);
uniform float rim_strength : hint_range(0.0, 2.0) = 0.9;
uniform float glow : hint_range(0.0, 1.0) = 0.0;   // hover / focus, animated from script
uniform float saturation = 1.3;

float sd_round_box(vec2 p, vec2 b, float r) {
	vec2 q = abs(p) - b + r;
	return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

vec3 grab(vec2 uv, float lod) {
	return textureLod(screen_tex, clamp(uv, vec2(0.001), vec2(0.999)), lod).rgb;
}

void fragment() {
	vec2 half_size = rect_size * 0.5;
	vec2 p = (UV - 0.5) * rect_size;
	float r = min(radius, min(half_size.x, half_size.y));
	float d = sd_round_box(p, half_size, r);
	float mask = 1.0 - smoothstep(-0.8, 0.8, d);

	vec2 e = vec2(0.75, 0.0);
	vec2 n = normalize(vec2(
		sd_round_box(p + e.xy, half_size, r) - sd_round_box(p - e.xy, half_size, r),
		sd_round_box(p + e.yx, half_size, r) - sd_round_box(p - e.yx, half_size, r)) + 1e-5);

	float depth = clamp(-d / bevel, 0.0, 1.0);
	float lens = pow(1.0 - depth, 2.2);
	vec2 px = SCREEN_PIXEL_SIZE * ui_scale;
	vec2 bend = n * refraction * lens * px;

	float spread = exp2(blur_lod) * 0.5;
	vec2 b = SCREEN_PIXEL_SIZE * spread;
	vec2 uv = SCREEN_UV - bend * (1.0 - dispersion * 0.5);
	vec3 col = grab(uv, blur_lod) * 0.4
		+ grab(uv + vec2(b.x, b.y), blur_lod) * 0.15
		+ grab(uv + vec2(-b.x, b.y), blur_lod) * 0.15
		+ grab(uv + vec2(b.x, -b.y), blur_lod) * 0.15
		+ grab(uv + vec2(-b.x, -b.y), blur_lod) * 0.15;
	col.r = mix(col.r, grab(SCREEN_UV - bend * (1.0 + dispersion), blur_lod).r, clamp(lens * 2.0, 0.0, 1.0));
	col.b = mix(col.b, grab(SCREEN_UV - bend * (1.0 - dispersion), blur_lod).b, clamp(lens * 2.0, 0.0, 1.0));

	float luma = dot(col, vec3(0.299, 0.587, 0.114));
	col = mix(vec3(luma), col, saturation);
	col = mix(col, tint.rgb, tint.a + glow * 0.07);

	vec2 light = normalize(vec2(-0.65, -0.75));
	float lit = pow(clamp(dot(n, light) * 0.5 + 0.5, 0.0, 1.0), 2.0)
		+ 0.4 * pow(clamp(dot(n, -light) * 0.5 + 0.5, 0.0, 1.0), 2.0);
	float line = 1.0 - smoothstep(0.0, 1.8, -d);
	float fresnel = pow(1.0 - depth, 3.0);
	float rim = (line * 0.85 + fresnel * 0.3) * lit * rim_strength * (1.0 + glow * 0.8);
	col += vec3(rim);

	col += vec3(0.06) * smoothstep(0.5, 0.0, UV.y) * (1.0 - fresnel);

	COLOR = vec4(col, mask);
}
