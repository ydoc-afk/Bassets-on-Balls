shader_type canvas_item;

// Slow-moving colour blobs for the glass panes to bend and blur.

uniform vec2 rect_size = vec2(1920.0, 1080.0);
uniform vec4 base : source_color = vec4(0.03, 0.035, 0.09, 1.0);
uniform vec4 colour_a : source_color = vec4(0.16, 0.30, 0.85, 1.0);
uniform vec4 colour_b : source_color = vec4(0.55, 0.20, 0.80, 1.0);
uniform vec4 colour_c : source_color = vec4(0.05, 0.65, 0.65, 1.0);
uniform float speed = 0.06;
uniform float time = 0.0; // fed from Backdrop.cs; reading TIME here would stop the UI from ever idling

float blob(vec2 p, vec2 centre, float radius) {
	float d = length(p - centre) / radius;
	return exp(-d * d);
}

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	float t = time * speed;
	float aspect = rect_size.x / rect_size.y;
	vec2 p = vec2(UV.x * aspect, UV.y);

	vec2 a = vec2(0.30 * aspect + 0.18 * sin(t * 1.3), 0.30 + 0.14 * cos(t * 1.1));
	vec2 b = vec2(0.75 * aspect + 0.20 * cos(t * 0.9), 0.70 + 0.16 * sin(t * 1.4));
	vec2 c = vec2(0.50 * aspect + 0.30 * sin(t * 0.7 + 2.0), 0.50 + 0.25 * cos(t * 0.8 + 1.0));

	vec3 col = base.rgb;
	col += colour_a.rgb * blob(p, a, 0.55) * 0.55;
	col += colour_b.rgb * blob(p, b, 0.50) * 0.50;
	col += colour_c.rgb * blob(p, c, 0.38) * 0.35;

	col += (hash(UV * rect_size) - 0.5) / 255.0 * 2.0;
	COLOR = vec4(col, 1.0);
}
