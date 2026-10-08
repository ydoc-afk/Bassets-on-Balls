shader_type canvas_item;

// XMB backdrop. Two glass strands chase each other round a shared axis (the gap between them breathes, so one
// seems to catch the other up), each a lit tube with a specular edge, fine banding, a glass rim, a thin filament
// braided round it and light pulses travelling along it. A big blurred pair sits behind for depth. Bokeh, drifting
// particles, light rays from the top right, a glossy floor with caustics. Drawn at half resolution by XmbBackdrop.

uniform vec2 rect_size = vec2(960.0, 540.0);
uniform vec3 colour_a = vec3(0.25, 0.45, 1.0);
uniform vec3 colour_b = vec3(0.1, 0.8, 0.9);
uniform vec3 strand0 = vec3(0.25, 0.45, 1.0); // near strands
uniform vec3 strand1 = vec3(0.1, 0.8, 0.9);
uniform vec3 strand2 = vec3(0.1, 0.8, 0.9);   // far strands
uniform vec3 strand3 = vec3(0.25, 0.45, 1.0);
uniform float time = 0.0; // fed from C#; reading TIME here would stop the UI from ever idling
uniform float nav = 0.0;  // where the menu is: the strands twist and the bokeh slides as you move around it

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// One lit glass tube. d: distance from its centre line, w: its radius, depth: cos of its phase (1 in front).
vec3 tube(float d, float w, float depth, vec3 base, float pulse, float along) {
	float t = d / w;
	float body = smoothstep(1.12, 0.88, abs(t));
	vec3 n = vec3(clamp(t, -1.0, 1.0), 0.0, 0.0);
	n.z = sqrt(max(0.0, 1.0 - n.x * n.x));
	vec3 light = normalize(vec3(-0.45, 0.65, 0.62));
	float diff = max(dot(n, light), 0.0);
	float spec = pow(max(dot(reflect(-light, n), vec3(0.0, 0.0, 1.0)), 0.0), 26.0);
	float rim = pow(1.0 - n.z, 2.4);
	float banding = 0.84 + 0.16 * sin(along * 46.0 + t * 2.0);
	vec3 solid = base * (0.15 + 0.85 * diff) * banding + base * rim * 0.95 + vec3(1.0) * spec * 0.95;
	vec3 glow = base * (exp(-t * t * 1.5) * 0.5 + exp(-t * t * 0.04) * 0.14);
	return (solid * body + glow) * (0.55 + 0.45 * depth) * (1.0 + pulse * 0.9);
}

// The pair of strands. soft > 1 widens and dims them (depth of field).
vec3 strands(vec2 uv, float aspect, float k, float amp_scale, float soft, float spin, vec3 ca, vec3 cb) {
	float x = uv.x * aspect;
	float base = x * k + time * 1.05 * spin + nav * 1.15;
	float ph1 = base;
	float ph2 = base + 3.14159 + 0.85 * sin(time * 0.33); // the gap breathes: the strands chase each other
	float yc = 0.50 + 0.05 * sin(uv.x * 1.4 + time * 0.07) - (uv.x - 0.5) * 0.16;
	float amp = (0.105 + 0.018 * sin(time * 0.25 + x * 2.0)) * amp_scale;
	float s1 = sin(ph1), c1 = cos(ph1), s2 = sin(ph2), c2 = cos(ph2);
	float y1 = yc + amp * s1, y2 = yc + amp * s2;
	float w1 = (0.0105 + 0.0065 * c1) * soft, w2 = (0.0105 + 0.0065 * c2) * soft;
	float p1 = pow(0.5 + 0.5 * sin(x * 2.6 - time * 1.1), 14.0);
	float p2 = pow(0.5 + 0.5 * sin(x * 2.6 - time * 1.1 + 3.1), 14.0);
	float hue = 0.85 + 0.15 * sin(x * 1.1 + time * 0.05);

	vec3 col = tube(uv.y - y1, w1, c1, ca * hue, p1, x * k) + tube(uv.y - y2, w2, c2, cb * hue, p2, x * k + 1.3);

	// A thin filament braided round each tube
	float f1 = y1 + 0.021 * soft * sin(ph1 * 3.0 + time * 1.7);
	float f2 = y2 + 0.021 * soft * sin(ph2 * 3.0 - time * 1.7);
	col += mix(ca, vec3(1.0), 0.55) * exp(-pow((uv.y - f1) / (0.0016 * soft), 2.0)) * 0.6 * (0.45 + 0.55 * c1) / soft;
	col += mix(cb, vec3(1.0), 0.55) * exp(-pow((uv.y - f2) / (0.0016 * soft), 2.0)) * 0.6 * (0.45 + 0.55 * c2) / soft;
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

// Small bright particles with a halo, drifting up. thresh: how sparse, size: radius within the cell.
vec3 motes(vec2 uv, float aspect, float scale, float speed, float parallax, float seed, float thresh, float size, float gain) {
	vec2 p = vec2(uv.x * aspect + nav * parallax, uv.y) * scale;
	p.y -= time * speed;
	p.x += sin(time * 0.2 + p.y * 0.7 + seed) * 0.15;
	vec2 id = floor(p), f = fract(p) - 0.5;
	float rn = hash(id + seed);
	if (rn < thresh) return vec3(0.0);
	vec2 off = (vec2(hash(id + seed + 3.0), hash(id + seed + 9.0)) - 0.5) * 0.4;
	float d = length(f - off);
	float tw = 0.45 + 0.55 * sin(time * (0.8 + rn * 3.0) + rn * 40.0);
	float core = smoothstep(size, 0.0, d);
	float halo = exp(-d * d / (size * size * 2.5)) * 0.30 * smoothstep(0.5, 0.32, max(abs(f.x), abs(f.y)));
	vec3 tint = mix(mix(colour_a, colour_b, hash(id + seed + 1.0)), vec3(1.0), 0.55);
	return tint * (core + halo) * tw * gain;
}

void fragment() {
	vec2 uv = vec2(UV.x, 1.0 - UV.y); // y up, like the prototype
	float aspect = rect_size.x / rect_size.y;
	vec3 col = mix(vec3(0.010, 0.014, 0.036), colour_a * 0.22, pow(1.0 - uv.y, 1.7));
	col += colour_b * 0.10 * exp(-length((uv - vec2(0.12, 0.0)) * vec2(1.0, 1.5)) * 2.2);

	// Keep the text column readable: the strands fade in to the right of it. Portrait screens push it further right.
	float lane = aspect > 1.2 ? smoothstep(0.30, 0.50, uv.x) * (1.0 - 0.45 * smoothstep(0.80, 1.0, uv.x))
	                          : smoothstep(0.30, 0.70, uv.x);
	float calm = mix(0.35, 1.0, lane); // bokeh, rays and particles behind the text are toned down

	// Light rays fanning out from a source above the top right corner, slowly turning
	vec2 sp = vec2(uv.x * aspect, uv.y) - vec2(0.92 * aspect, 1.25);
	float ang = atan(sp.y, sp.x);
	float rays = pow(0.5 + 0.5 * sin(ang * 13.0 + time * 0.13), 3.0) * 0.55
	           + pow(0.5 + 0.5 * sin(ang * 29.0 - time * 0.09 + 1.3), 5.0) * 0.40
	           + pow(0.5 + 0.5 * sin(ang * 7.0 + time * 0.05 + 2.0), 2.0) * 0.45;
	rays *= 0.65 + 0.35 * sin(length(sp) * 5.0 - time * 0.4);
	col += mix(colour_b, vec3(1.0), 0.55) * rays * exp(-length(sp) * 0.85) * 0.24 * calm;

	// Far depth: a big blurred pair
	col += strands(vec2(uv.x * 0.7 + 0.12, uv.y - 0.07), aspect, 4.5, 1.55, 3.2, 0.5, strand2, strand3) * 0.17 * lane;
	col += bokeh(uv, aspect, 4.0, 0.016, 0.30, 0.85, 11.0, 0.5) * calm;

	// In focus: glass with dispersion (red and blue sit a hair apart along the vertical)
	float disp = 0.0028;
	vec3 fr = strands(uv + vec2(0.0, disp), aspect, 9.0, 1.0, 1.0, 1.0, strand0, strand1);
	vec3 fg = strands(uv, aspect, 9.0, 1.0, 1.0, 1.0, strand0, strand1);
	vec3 fb = strands(uv - vec2(0.0, disp), aspect, 9.0, 1.0, 1.0, 1.0, strand0, strand1);
	col += vec3(fr.r, fg.g, fb.b) * 0.95 * lane;

	col += bokeh(uv, aspect, 7.0, 0.030, 0.62, 0.55, 23.0, 0.6) * calm;

	// Glossy floor: the strands mirrored below a horizon, rippling and fading with distance, with caustics on it
	float fy = 0.16;
	if (uv.y < fy) {
		float depth = fy - uv.y;
		vec2 ru = vec2(uv.x + 0.0025 * sin(uv.y * 90.0 + time * 1.2), 2.0 * fy - uv.y);
		col += strands(ru, aspect, 9.0, 1.0, 1.0 + depth * 9.0, 1.0, strand0, strand1) * 0.32 * lane * exp(-depth * 7.0);
		float cau = pow(0.5 + 0.5 * sin(uv.x * aspect * 26.0 + time * 0.8 + sin(uv.y * 44.0 + time) * 1.6), 6.0);
		col += mix(colour_a, colour_b, 0.5) * cau * exp(-depth * 11.0) * 0.07 * lane;
	}
	col += mix(colour_a, colour_b, 0.5) * exp(-pow((uv.y - fy) / 0.003, 2.0)) * 0.18;

	// Near depth: sharper discs, then four layers of particles at different distances
	col += bokeh(uv, aspect, 12.0, 0.055, 1.0, 0.25, 37.0, 0.5) * calm;
	col += motes(uv, aspect, 9.0, 0.030, 0.40, 91.0, 0.70, 0.12, 0.85) * calm;
	col += motes(uv, aspect, 16.0, 0.050, 0.55, 51.0, 0.62, 0.09, 0.60) * calm;
	col += motes(uv, aspect, 26.0, 0.070, 0.80, 63.0, 0.60, 0.08, 0.50) * calm;
	col += motes(uv, aspect, 41.0, 0.095, 1.10, 77.0, 0.58, 0.07, 0.45) * calm;

	col *= 1.0 - 0.55 * pow(length(uv - 0.5) * 1.25, 2.0);
	col += (hash(UV * rect_size) - 0.5) / 255.0 * 2.0; // dither so the gradients don't band in the stream
	COLOR = vec4(col, 1.0);
}
