/*{
    "DESCRIPTION": "Smoke dissipation - content wisps away like rising smoke with animated turbulence and gradient mask",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "amount", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "rise", "TYPE": "float", "MIN": -1.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "drift", "TYPE": "float", "MIN": -1.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "turbulence", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.6 },
        { "NAME": "scale", "TYPE": "float", "MIN": 1.0, "MAX": 15.0, "DEFAULT": 4.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.4 },
        { "NAME": "fade", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "wisp", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.4 },
        { "NAME": "glow", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 0.5, "DEFAULT": 0.2 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
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
    for (int i = 0; i < 6; i++) {
        v += a * noise(p);
        p *= 2.0;
        a *= 0.5;
    }
    return v;
}

float luma(vec3 c) {
    return dot(c, vec3(0.299, 0.587, 0.114));
}

// Curl noise for smooth swirling smoke motion
vec2 curlNoise(vec2 p) {
    float eps = 0.5;
    float n1 = fbm(p + vec2(eps, 0.0));
    float n2 = fbm(p - vec2(eps, 0.0));
    float n3 = fbm(p + vec2(0.0, eps));
    float n4 = fbm(p - vec2(0.0, eps));
    return vec2((n3 - n4), -(n1 - n2)) / (2.0 * eps);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 original = IMG_NORM_PIXEL(inputImage, uv);

    // Gradient mask
    float mask;
    if (!radial) {
        vec2 p = uv - 0.5;
        float cosA = cos(angle);
        float sinA = sin(angle);
        float axis = (p.x * cosA + p.y * sinA) + 0.5;
        mask = smoothstep(maskWidth, maskWidth + fadeWidth, abs(axis - maskPos));
    } else {
        vec2 p = uv - vec2(maskPos, 0.5);
        p.x *= RENDERSIZE.x / RENDERSIZE.y;
        mask = smoothstep(maskWidth, maskWidth + fadeWidth, length(p));
    }
    if (invert) mask = 1.0 - mask;

    float effectAmt = mask * amount;
    if (effectAmt < 0.001) {
        gl_FragColor = original;
        return;
    }

    vec2 px = 1.0 / RENDERSIZE;
    float lum = luma(original.rgb);
    float amt = effectAmt * effectAmt;
    float t = TIME * speed;

    // Base smoke direction (rise + drift)
    vec2 smokeDir = normalize(vec2(drift, rise) + vec2(0.001));

    // === Multi-layer animated turbulence ===

    // Layer 1: large-scale swirling flow (slowest)
    vec2 curl1 = curlNoise(uv * scale * 0.6 + t * 0.15);

    // Layer 2: medium turbulence with domain warping
    float t1x = fbm(uv * scale + t * 0.2) * 2.0 - 1.0;
    float t1y = fbm(uv * scale + 50.0 - t * 0.15) * 2.0 - 1.0;
    vec2 warped = uv * scale + vec2(t1x, t1y) * turbulence * 0.5;
    float t2x = fbm(warped + 20.0 + t * 0.1) * 2.0 - 1.0;
    float t2y = fbm(warped + 70.0 - t * 0.12) * 2.0 - 1.0;

    // Layer 3: fine curl noise detail (fastest)
    vec2 curl2 = curlNoise(uv * scale * 2.0 + 30.0 + t * 0.3);

    // Blend turbulence layers
    vec2 turbDir = vec2(
        mix(t1x, t2x, turbulence) * turbulence,
        mix(t1y, t2y, turbulence) * turbulence
    );
    turbDir += curl1 * turbulence * 0.8;
    turbDir += curl2 * turbulence * 0.3;

    // === Multi-pass displacement for longer smoke trails ===

    // Pass 1: main displacement
    vec2 displacement = (smokeDir * (1.0 - turbulence * 0.5) + turbDir) * amt * 0.2;

    // Pass 2: secondary displacement at warped position for longer trails
    vec2 sampleUV1 = uv - displacement;
    vec2 curl3 = curlNoise(sampleUV1 * scale * 1.2 + 60.0 - t * 0.2);
    vec2 disp2 = (smokeDir * 0.5 + curl3 * turbulence) * amt * 0.1;
    vec2 sampleUV = sampleUV1 - disp2;

    vec4 smokeColor = IMG_NORM_PIXEL(inputImage, sampleUV);
    float smokeLum = luma(smokeColor.rgb);

    // === Animated wisp effect: tendrils break apart over time ===
    float wispNoise = fbm(uv * scale * 1.5 + displacement * 8.0 + t * 0.25);
    float wispNoise2 = fbm(uv * scale * 2.5 + displacement * 5.0 - t * 0.35);
    float combinedWisp = mix(wispNoise, wispNoise2, 0.4);
    float wispMask = smoothstep(wisp * amt, wisp * amt + 0.25, combinedWisp);

    // Opacity fading: smoke thins out as it travels
    float travelDist = length(displacement + disp2);
    float opacity = 1.0 - smoothstep(0.0, 0.1 + (1.0 - fade) * 0.2, travelDist);

    // Near-source pixels dissolve based on amount
    float dissolve = smoothstep(amt * 0.7, amt * 0.7 + 0.15, lum * (0.3 + combinedWisp * 0.7));

    // Combine: displaced smoke with fading and wisping
    vec3 col = smokeColor.rgb * opacity * wispMask;

    // Glow: bright edges where smoke is dense
    float edgeGlow = smoothstep(0.3, 0.0, abs(wispMask - 0.5)) * glow * smokeLum;
    col += col * edgeGlow * 3.0;

    // Blend: undissolved original + smoke wisps
    vec3 result = mix(col, original.rgb, dissolve * (1.0 - amt));

    float alpha = max(dissolve * (1.0 - amt), opacity * wispMask * smokeLum) * original.a;

    gl_FragColor = vec4(mix(original.rgb, result, effectAmt), mix(original.a, alpha, effectAmt));
}
