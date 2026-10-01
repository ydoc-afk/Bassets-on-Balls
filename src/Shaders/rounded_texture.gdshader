shader_type canvas_item;

uniform vec2 rect_size = vec2(100.0);
uniform float radius = 24.0;

void fragment() {
	vec2 q = abs((UV - 0.5) * rect_size) - rect_size * 0.5 + radius;
	float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
	COLOR.a *= 1.0 - smoothstep(-0.8, 0.8, d);
}
