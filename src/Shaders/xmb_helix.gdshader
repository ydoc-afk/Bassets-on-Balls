shader_type canvas_item;

// XMB backdrop: a translucent double helix (two glass tubes with a specular rim, rungs between them), mirrored in a
// glossy floor, with a few drifting sparks and a vignette. Drawn at half resolution by XmbBackdrop.

uniform vec2 rect_size = vec2(960.0, 540.0);
uniform vec3 colour_a = vec3(0.25, 0.45, 1.0);
uniform vec3 colour_b = vec3(0.1, 0.8, 0.9);
uniform float time = 0.0; // fed from C#; reading TIME here would stop the UI from ever idling

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// The strand whose cos() is positive is in front, so it is thicker and brighter.
vec3 helix(vec2 uv, float aspect) {
	float x = uv.x * aspect, k = 9.0, ph = x * k + time * 0.55;
	float yc = 0.50 + 0.05 * sin(uv.x * 1.4 + time * 0.05) - (uv.x - 0.5) * 0.16;
	float amp = 0.105 + 0.015 * sin(time * 0.2 + x * 2.0);
	float s = sin(ph), c = cos(ph);
	float y1 = yc + amp * s, y2 = yc - amp * s;
	float d1 = uv.y - y1, d2 = uv.y - y2;
	float w1 = 0.0085 + 0.0045 * c, w2 = 0.0085 - 0.0045 * c;
	float i1 = 0.6 + 0.4 * c, i2 = 0.6 - 0.4 * c;
	vec3 c1 = mix(colour_a, colour_b, 0.5 + 0.5 * sin(x * 1.1));
	vec3 c2 = mix(colour_b, colour_a, 0.5 + 0.5 * sin(x * 1.1 + 1.0));
	vec3 col = vec3(0.0);
	col += c1 * (exp(-d1 * d1 / (w1 * w1)) * 0.50 + exp(-d1 * d1 / (w1 * w1 * 40.0)) * 0.16) * i1;
	col += vec3(1.0) * exp(-pow((d1 - w1 * 0.5) / (w1 * 0.30), 2.0)) * 0.34 * i1;
	col += c2 * (exp(-d2 * d2 / (w2 * w2)) * 0.50 + exp(-d2 * d2 / (w2 * w2 * 40.0)) * 0.16) * i2;
	col += vec3(1.0) * exp(-pow((d2 - w2 * 0.5) / (w2 * 0.30), 2.0)) * 0.34 * i2;
	float u = fract(ph / 1.0472), rx = (u - 0.5) * 1.0472 / k;
	float rung = exp(-rx * rx / (0.0018 * 0.0018));
	float inside = smoothstep(0.0, 0.004, uv.y - min(y1, y2)) * smoothstep(0.0, 0.004, max(y1, y2) - uv.y);
	col += mix(c1, c2, uv.y) * rung * inside * 0.30;
	return col;
}

void fragment() {
	vec2 uv = vec2(UV.x, 1.0 - UV.y); // y up, like the prototype
	float aspect = rect_size.x / rect_size.y;
	vec3 col = mix(vec3(0.012, 0.016, 0.04), colour_a * 0.22, pow(1.0 - uv.y, 1.7));
	col += colour_b * 0.10 * exp(-length((uv - vec2(0.12, 0.0)) * vec2(1.0, 1.5)) * 2.2);

	// Keep the text column readable: the strand fades in to the right of it. Portrait screens push it further right.
	float lane = aspect > 1.2 ? smoothstep(0.30, 0.50, uv.x) * (1.0 - 0.45 * smoothstep(0.80, 1.0, uv.x))
	                          : smoothstep(0.30, 0.70, uv.x);
	col += helix(uv, aspect) * 0.95 * lane;

	// Glossy floor: mirror the strand below a horizon, fading with distance and rippling slightly.
	float fy = 0.16;
	if (uv.y < fy) {
		vec2 ru = vec2(uv.x + 0.002 * sin(uv.y * 90.0 + time), 2.0 * fy - uv.y);
		col += helix(ru, aspect) * 0.30 * lane * exp(-(fy - uv.y) * 7.0);
	}
	col += mix(colour_a, colour_b, 0.5) * exp(-pow((uv.y - fy) / 0.003, 2.0)) * 0.16;
	col += vec3(1.0) * 0.025 * smoothstep(0.55, 0.0, abs(uv.x - uv.y * 0.9 - 0.25)); // soft diagonal sheen

	for (int l = 0; l < 2; l++) {
		float fl = float(l);
		vec2 q = vec2(uv.x * aspect, uv.y) * (13.0 + 9.0 * fl);
		q.y -= time * (0.06 + 0.03 * fl);
		vec2 id = floor(q), f = fract(q) - 0.5, r = vec2(hash(id + 7.0 + fl), hash(id + 13.0 + fl)) - 0.5;
		float rn = hash(id + fl), dd = length(f - r * 0.6);
		float twinkle = 0.5 + 0.5 * sin(time * (1.0 + rn * 2.0) + rn * 6.28);
		col += vec3(0.8, 0.9, 1.0) * smoothstep(0.07, 0.0, dd) * twinkle * step(0.8, rn) * 0.4;
	}

	col *= 1.0 - 0.55 * pow(length(uv - 0.5) * 1.25, 2.0);
	col += (hash(UV * rect_size) - 0.5) / 255.0 * 2.0; // dither so the gradients don't band in the stream
	COLOR = vec4(col, 1.0);
}
