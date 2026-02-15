/*{
    "DESCRIPTION": "Organic shape generator - dots, blobs, and strokes",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        { "NAME": "dots", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "blobs", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "strokes", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "scale", "TYPE": "float", "MIN": 0.2, "MAX": 4.0, "DEFAULT": 1.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "complexity", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "seed", "TYPE": "float", "MIN": 0.0, "MAX": 100.0, "DEFAULT": 0.0 }
    ]
}*/

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float hash1(float p) {
    return fract(sin(p * 127.1) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float fbm(vec2 p) {
    float v = 0.0, a = 0.5;
    mat2 rot = mat2(0.8, 0.6, -0.6, 0.8);
    for (int i = 0; i < 5; i++) {
        v += a * noise(p);
        p = rot * p * 2.0;
        a *= 0.5;
    }
    return v;
}

// Smooth minimum for metaball merging
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// === GROUP 1: DOTS ===
// Scattered circles of varying size, some in chains
float dotsLayer(vec2 uv, float t, float s) {
    float result = 1e10;

    // Layer 1: scattered individual dots
    for (int layer = 0; layer < 3; layer++) {
        float fl = float(layer);
        float gridScale = (3.0 + fl * 4.0) * s;
        vec2 cell = floor(uv * gridScale);
        vec2 local = fract(uv * gridScale) - 0.5;

        float h = hash(cell + fl * 137.0 + seed);
        float h2 = hash(cell + fl * 237.0 + seed);
        // Sparse placement
        float threshold = 0.7 - complexity * 0.3;
        if (h > threshold) {
            // Jitter position
            vec2 offset = vec2(hash(cell + 11.0) - 0.5, hash(cell + 23.0) - 0.5) * 0.6;
            float r = 0.08 + h2 * 0.25;
            float d = length(local - offset) - r;
            result = min(result, d);
        }
    }

    // Layer 2: dot chains - dots along a noisy path
    for (int chain = 0; chain < 3; chain++) {
        float fc = float(chain);
        float yBase = (fc - 1.0) * 0.35 + hash1(fc + seed) * 0.2;
        float dotSpacing = 0.03 + hash1(fc * 7.0 + seed) * 0.04;
        float chainLen = 8.0 + hash1(fc * 13.0 + seed) * 15.0;

        for (int d = 0; d < 20; d++) {
            float fd = float(d);
            if (fd > chainLen) break;
            float xPos = -0.5 + fd * dotSpacing + hash1(fc * 3.0 + seed) * 0.3;
            float yPos = yBase + noise(vec2(xPos * 4.0 + fc * 10.0, t * 0.5)) * 0.08;
            float r = 0.004 + hash1(fd * 3.0 + fc * 17.0 + seed) * 0.008;
            // Grow dots along chain
            r *= smoothstep(0.0, 3.0, fd) * smoothstep(chainLen, chainLen - 3.0, fd);
            float dist = length(uv - vec2(xPos, yPos)) - r;
            result = min(result, dist);
        }
    }

    return 1.0 - smoothstep(0.0, 0.003, result);
}

// === GROUP 2: BLOBS ===
// Organic metaball shapes, connected masses
float blobsLayer(vec2 uv, float t, float s) {
    float result = 1e10;

    // Multiple blob clusters
    for (int cluster = 0; cluster < 3; cluster++) {
        float fc = float(cluster);
        vec2 clusterCenter = vec2(
            hash1(fc * 7.0 + seed + 0.5) * 1.6 - 0.8,
            (fc - 1.0) * 0.3 + hash1(fc * 11.0 + seed) * 0.2
        );

        // Metaball field for this cluster
        float field = 0.0;
        int numBalls = 4 + int(complexity * 5.0);
        for (int i = 0; i < 9; i++) {
            if (i >= numBalls) break;
            float fi = float(i);
            vec2 pos = clusterCenter + vec2(
                noise(vec2(fi * 7.3 + seed, t * 0.4 + fc * 20.0)) * 0.25 - 0.125,
                noise(vec2(fi * 13.7 + seed, t * 0.35 + fc * 20.0 + 100.0)) * 0.15 - 0.075
            );
            float r = 0.015 + noise(vec2(fi * 3.1 + seed, t * 0.2 + fc * 30.0)) * 0.04;
            r *= (1.0 + complexity * 0.5);
            field += r * r / (dot(uv - pos, uv - pos) + 0.0001);
        }
        // Threshold for metaball surface
        float blobDist = 0.8 - field;
        result = min(result, blobDist);
    }

    // Add some drip/tendril shapes hanging from blobs
    for (int drip = 0; drip < 4; drip++) {
        float fd = float(drip);
        float xPos = hash1(fd * 17.0 + seed + 3.0) * 1.4 - 0.7;
        float yTop = hash1(fd * 23.0 + seed + 5.0) * 0.3 - 0.05;
        float dripLen = 0.05 + hash1(fd * 31.0 + seed) * 0.15;
        float width = 0.003 + hash1(fd * 37.0 + seed) * 0.006;

        // Curved drip
        for (float s2 = 0.0; s2 < 1.0; s2 += 0.05) {
            float yPos = yTop - s2 * dripLen;
            float xOff = noise(vec2(s2 * 8.0 + fd * 10.0, t * 0.3)) * 0.02;
            float w = width * (1.0 - s2 * 0.7); // Taper
            float d = length(uv - vec2(xPos + xOff, yPos)) - w;
            result = min(result, d);
        }
    }

    return 1.0 - smoothstep(0.0, 0.003, result);
}

// === GROUP 3: STROKES ===
// Calligraphic curves, swooshes, thin lines
float strokesLayer(vec2 uv, float t, float s) {
    float result = 0.0;

    // Flowing curves
    for (int curve = 0; curve < 5; curve++) {
        float fc = float(curve);
        float yBase = (fc - 2.0) * 0.22 + hash1(fc * 19.0 + seed) * 0.15;
        float freq = 2.0 + hash1(fc * 7.0 + seed) * 4.0;
        float amp = 0.03 + hash1(fc * 11.0 + seed) * 0.08;

        // Curve y position at this x
        float curveY = yBase +
            sin(uv.x * freq + t * 0.8 + fc * 2.0) * amp * 0.5 +
            noise(vec2(uv.x * freq * 0.5 + fc * 10.0 + seed, t * 0.3)) * amp;

        // Variable thickness - calligraphic feel
        float baseWidth = 0.001 + hash1(fc * 23.0 + seed) * 0.004;
        float thickness = baseWidth + noise(vec2(uv.x * 8.0 + fc * 15.0, t * 0.2)) * 0.006 * complexity;

        // Fade at ends
        float xRange = 0.15 + hash1(fc * 29.0 + seed) * 0.5;
        float xCenter = hash1(fc * 31.0 + seed) * 1.2 - 0.6;
        float fade = smoothstep(xRange, xRange * 0.3, abs(uv.x - xCenter));

        float d = abs(uv.y - curveY) - thickness;
        float stroke = (1.0 - smoothstep(0.0, 0.002, d)) * fade;
        result = max(result, stroke);
    }

    // Zigzag / angular marks
    for (int zig = 0; zig < 3; zig++) {
        float fz = float(zig);
        float yBase = hash1(fz * 41.0 + seed + 10.0) * 0.8 - 0.4;
        float xStart = hash1(fz * 43.0 + seed + 10.0) * 1.0 - 0.5;
        float segLen = 0.02 + hash1(fz * 47.0 + seed) * 0.04;
        int numSegs = 4 + int(complexity * 8.0);

        vec2 prev = vec2(xStart, yBase);
        for (int seg = 0; seg < 12; seg++) {
            if (seg >= numSegs) break;
            float fs = float(seg);
            vec2 next = prev + vec2(
                segLen * (0.5 + hash1(fs * 3.0 + fz * 50.0 + seed) * 0.5),
                (hash1(fs * 7.0 + fz * 60.0 + seed) - 0.5) * segLen * 2.0
            );

            // Distance to line segment
            vec2 pa = uv - prev;
            vec2 ba = next - prev;
            float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
            float d = length(pa - ba * h) - 0.002;
            result = max(result, 1.0 - smoothstep(0.0, 0.002, d));

            prev = next;
        }
    }

    // Arc / swoosh shapes
    for (int arc = 0; arc < 2; arc++) {
        float fa = float(arc);
        vec2 center = vec2(
            hash1(fa * 53.0 + seed + 20.0) * 1.2 - 0.6,
            hash1(fa * 59.0 + seed + 20.0) * 0.6 - 0.3
        );
        float radius = 0.05 + hash1(fa * 61.0 + seed) * 0.12;
        float arcWidth = 0.002 + hash1(fa * 67.0 + seed) * 0.003;
        float startAngle = hash1(fa * 71.0 + seed) * 6.28;
        float sweep = 1.0 + hash1(fa * 73.0 + seed) * 3.0;

        vec2 d = uv - center;
        float ang = atan(d.y, d.x);
        // Wrap angle relative to start
        float angDiff = mod(ang - startAngle + 6.28, 6.28);
        float arcMask = smoothstep(0.0, 0.3, angDiff) * smoothstep(sweep, sweep - 0.3, angDiff);

        float ringDist = abs(length(d) - radius) - arcWidth;
        float arcStroke = (1.0 - smoothstep(0.0, 0.002, ringDist)) * arcMask;
        result = max(result, arcStroke);
    }

    return result;
}

void main() {
    vec2 uv = isf_FragNormCoord - 0.5;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    uv /= scale;

    float t = TIME * speed;

    float col = 0.0;

    if (dots > 0.01)
        col = max(col, dotsLayer(uv, t, scale) * dots);

    if (blobs > 0.01)
        col = max(col, blobsLayer(uv, t, scale) * blobs);

    if (strokes > 0.01)
        col = max(col, strokesLayer(uv, t, scale) * strokes);

    col = clamp(col, 0.0, 1.0);
    gl_FragColor = vec4(vec3(col), 1.0);
}
