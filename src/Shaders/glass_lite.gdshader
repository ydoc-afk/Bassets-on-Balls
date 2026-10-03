shader_type canvas_item;

// Cheap stand-in for liquid_glass.gdshader: no screen read, just a translucent gradient with the same lit rim.

uniform vec2 rect_size = vec2(300.0, 400.0);
uniform float radius = 32.0;
uniform float bevel = 22.0;
uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 0.09);
uniform float rim_strength : hint_range(0.0, 2.0) = 0.9;
uniform float glow : hint_range(0.0, 1.0) = 0.0;

float sd_round_box(vec2 p, vec2 b, float r) {
	vec2 q = abs(p) - b + r;
	return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
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
	vec3 col = mix(vec3(0.58, 0.68, 1.0), vec3(0.28, 0.34, 0.68), UV.y);
	col = mix(col, tint.rgb, tint.a * 0.5);

	vec2 light = normalize(vec2(-0.65, -0.75));
	float lit = pow(clamp(dot(n, light) * 0.5 + 0.5, 0.0, 1.0), 2.0)
		+ 0.4 * pow(clamp(dot(n, -light) * 0.5 + 0.5, 0.0, 1.0), 2.0);
	float line = 1.0 - smoothstep(0.0, 1.8, -d);
	float fresnel = pow(1.0 - depth, 3.0);
	float rim = (line * 0.85 + fresnel * 0.3) * lit * rim_strength * (1.0 + glow * 0.8);

	float alpha = clamp(0.34 + tint.a + glow * 0.12 + rim * 0.4, 0.0, 1.0);
	COLOR = vec4(col + vec3(rim), mask * alpha);
}
