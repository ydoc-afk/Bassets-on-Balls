shader_type canvas_item;

// XMB backdrop. Three depths: a big out-of-focus helix behind, the glass double helix in focus (dispersion on the
// edges, light pulses running along the tubes, rungs), and bokeh discs drifting at several distances. Light shafts
// from the top right, a glossy floor with caustics and a mirrored strand. Drawn at half resolution by XmbBackdrop.

uniform vec2 rect_size = vec2(960.0, 540.0);
uniform vec3 colour_a = vec3(0.25, 0.45, 1.0);
uniform vec3 colour_b = vec3(0.1, 0.8, 0.9);
uniform float time = 0.0; // fed from C#; reading TIME here would stop the UI from ever idling
uniform float nav = 0.0;  // where the menu is: the strand twists and the bokeh slides as you move around it

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// One strand pair. soft > 1 widens and dims it (depth of field); the strand whose cos() is positive is in front.
vec3 helix(vec2 uv, float aspect, float k, float amp_scale, float soft, float spin) {
	float x = uv.x * aspect, ph = x * k + time * 0.9 * spin + nav * 1.15;
	float yc = 0.50 + 0.05 * sin(uv.x * 1.4 + time * 0.07) - (uv.x - 0.5) * 0.16;
	float amp = (0.105 + 0.018 * sin(time * 0.25 + x * 2.0)) * amp_scale;
	float s = sin(ph), c = cos(ph);
	float y1 = yc + amp * s, y2 = yc - amp * s;
	float d1 = uv.y - y1, d2 = uv.y - y2;
	float w1 = (0.0085 + 0.0050 * c) * soft, w2 = (0.0085 - 0.0050 * c) * soft;
	float i1 = 0.6 + 0.4 * c, i2 = 0.6 - 0.4 * c;
	vec3 c1 = mix(colour_a, colour_b, 0.5 + 0.5 * sin(x * 1.1 + time * 0.05));
	vec3 c2 = mix(colour_b, colour_a, 0.5 + 0.5 * sin(x * 1.1 + 1.0 + time * 0.05));

	// light pulses travelling along the tubes
	float p1 = pow(0.5 + 0.5 * sin(x * 2.6 - time * 1.1), 14.0);
	float p2 = pow(0.5 + 0.5 * sin(x * 2.6 - time * 1.1 + 3.1), 14.0);

	vec3 col = vec3(0.0);
	col += c1 * (exp(-d1 * d1 / (w1 * w1)) * (0.50 + 0.55 * p1) + exp(-d1 * d1 / (w1 * w1 * 40.0)) * (0.16 + 0.12 * p1)) * i1;
	col += vec3(1.0) * exp(-pow((d1 - w1 * 0.5) / (w1 * 0.30), 2.0)) * (0.34 + 0.35 * p1) * i1 / soft;
	col += c2 * (exp(-d2 * d2 / (w2 * w2)) * (0.50 + 0.55 * p2) + exp(-d2 * d2 / (w2 * w2 * 40.0)) * (0.16 + 0.12 * p2)) * i2;
	col += vec3(1.0) * exp(-pow((d2 - w2 * 0.5) / (w2 * 0.30), 2.0)) * (0.34 + 0.35 * p2) * i2 / soft;

	float u = fract(ph / 1.0472), rx = (u - 0.5) * 1.0472 / k;
	float rung = exp(-rx * rx / (0.0018 * 0.0018 * soft));
	float inside = smoothstep(0.0, 0.004 * soft, uv.y - min(y1, y2)) * smoothstep(0.0, 0.004 * soft, max(y1, y2) - uv.y);
	col += mix(c1, c2, uv.y) * rung * inside * 0.30 / soft;
	return col;
}

// Out-of-focus discs: a soft body with a brighter rim, like light through a wide-open lens.
vec3 bokeh(vec2 uv, float aspect, float scale, float speed, float parallax, float blur, float seed, float amount) {
	vec2 p = vec2(uv.x * aspect + nav * parallax, uv.y) * scale;
	p.y -= time * speed;
	p.x += sin(time * 0.11 + seed) * 0.35;
	vec2 id = floor(p), f = fract(p) - 0.5;
	float rn = hash(id + seed);
	if (rn < 0.52) return vec3(0.0);
	vec2 off = (vec2(hash(id + seed + 3.0), hash(id + seed + 9.0)) - 0.5) * 0.36;
	float r = 0.10 + 0.17 * hash(id + seed + 5.0);
	float d = length(f - off);
	float body = 1.0 - smoothstep(r * (1.0 - blur), r, d);
	float rim = exp(-pow((d - r * 0.9) / (r * 0.13), 2.0)) * (1.0 - blur * 0.6);
	float tw = 0.55 + 0.45 * sin(time * (0.35 + rn) + rn * 6.283);
	vec3 tint = mix(mix(colour_a, colour_b, hash(id + seed + 1.0)), vec3(1.0), 0.35);
	return tint * (body * 0.34 + rim * 0.5) * tw * amount * (rn - 0.4);
}

void fragment() {
	vec2 uv = vec2(UV.x, 1.0 - UV.y); // y up, like the prototype
	float aspect = rect_size.x / rect_size.y;
	vec3 col = mix(vec3(0.010, 0.014, 0.036), colour_a * 0.22, pow(1.0 - uv.y, 1.7));
	col += colour_b * 0.10 * exp(-length((uv - vec2(0.12, 0.0)) * vec2(1.0, 1.5)) * 2.2);

	// Keep the text column readable: the strand fades in to the right of it. Portrait screens push it further right.
	float lane = aspect > 1.2 ? smoothstep(0.30, 0.50, uv.x) * (1.0 - 0.45 * smoothstep(0.80, 1.0, uv.x))
	                          : smoothstep(0.30, 0.70, uv.x);
	float calm = mix(0.35, 1.0, lane); // bokeh and light behind the text are toned down

	// Light shafts from the top right, drifting slowly
	for (int i = 0; i < 2; i++) {
		float fi = float(i);
		float line = uv.x - (0.78 - 0.30 * fi) + uv.y * (0.50 + 0.12 * fi) + 0.05 * sin(time * 0.06 + fi * 2.0);
		float shaft = pow(max(0.0, 1.0 - abs(line) * (2.6 + fi)), 3.0) * (0.6 + 0.4 * sin(uv.y * 6.0 + time * 0.25 + fi));
		col += mix(colour_b, vec3(1.0), 0.25 + 0.2 * fi) * shaft * 0.055 * uv.y * calm;
	}

	// Far depth: a big blurred strand
	col += helix(vec2(uv.x * 0.7 + 0.12, uv.y - 0.07), aspect, 4.5, 1.55, 3.2, 0.5) * 0.42 * lane;
	col += bokeh(uv, aspect, 4.0, 0.016, 0.30, 0.85, 11.0, 0.5) * calm;

	// In focus: glass with dispersion (red and blue sit a hair apart along the vertical)
	float disp = 0.0028;
	vec3 fr = helix(uv + vec2(0.0, disp), aspect, 9.0, 1.0, 1.0, 1.0);
	vec3 fg = helix(uv, aspect, 9.0, 1.0, 1.0, 1.0);
	vec3 fb = helix(uv - vec2(0.0, disp), aspect, 9.0, 1.0, 1.0, 1.0);
	col += vec3(fr.r, fg.g, fb.b) * 0.95 * lane;

	col += bokeh(uv, aspect, 7.0, 0.030, 0.62, 0.55, 23.0, 0.6) * calm;

	// Glossy floor: the strand mirrored below a horizon, rippling and fading with distance, with caustics on it
	float fy = 0.16;
	if (uv.y < fy) {
		float depth = fy - uv.y;
		vec2 ru = vec2(uv.x + 0.0025 * sin(uv.y * 90.0 + time * 1.2), 2.0 * fy - uv.y);
		col += helix(ru, aspect, 9.0, 1.0, 1.0 + depth * 9.0, 1.0) * 0.32 * lane * exp(-depth * 7.0);
		float cau = pow(0.5 + 0.5 * sin(uv.x * aspect * 26.0 + time * 0.8 + sin(uv.y * 44.0 + time) * 1.6), 6.0);
		col += mix(colour_a, colour_b, 0.5) * cau * exp(-depth * 11.0) * 0.07 * lane;
	}
	col += mix(colour_a, colour_b, 0.5) * exp(-pow((uv.y - fy) / 0.003, 2.0)) * 0.18;
	col += vec3(1.0) * 0.022 * smoothstep(0.55, 0.0, abs(uv.x - uv.y * 0.9 - 0.25)); // soft diagonal sheen

	// Near depth: sharper discs in front of everything, plus a few dust motes
	col += bokeh(uv, aspect, 12.0, 0.055, 1.0, 0.25, 37.0, 0.5) * calm;
	for (int l = 0; l < 2; l++) {
		float fl = float(l);
		vec2 q = vec2(uv.x * aspect, uv.y) * (19.0 + 9.0 * fl);
		q.y -= time * (0.07 + 0.03 * fl);
		vec2 id = floor(q), f = fract(q) - 0.5, r = vec2(hash(id + 7.0 + fl), hash(id + 13.0 + fl)) - 0.5;
		float rn = hash(id + fl), dd = length(f - r * 0.6);
		float twinkle = 0.5 + 0.5 * sin(time * (1.0 + rn * 2.0) + rn * 6.28);
		col += vec3(0.8, 0.9, 1.0) * smoothstep(0.07, 0.0, dd) * twinkle * step(0.82, rn) * 0.4;
	}

	col *= 1.0 - 0.55 * pow(length(uv - 0.5) * 1.25, 2.0);
	col += (hash(UV * rect_size) - 0.5) / 255.0 * 2.0; // dither so the gradients don't band in the stream
	COLOR = vec4(col, 1.0);
}
