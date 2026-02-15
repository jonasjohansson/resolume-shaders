/*{
    "DESCRIPTION": "Watercolor paint effect with gradient mask - Kuwahara painterly smoothing mimicking Resolume Acuarela",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "strength", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 1.0 },
        { "NAME": "radius", "TYPE": "float", "MIN": 1.0, "MAX": 8.0, "DEFAULT": 4.0 },
        { "NAME": "bleed", "TYPE": "float", "MIN": 0.0, "MAX": 80.0, "DEFAULT": 8.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "spread", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "edgeDarken", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "paperGrain", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 0.5, "DEFAULT": 0.2 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "blendMask", "TYPE": "bool", "DEFAULT": false }
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
    for (int i = 0; i < 5; i++) {
        v += a * noise(p);
        p *= 2.0;
        a *= 0.5;
    }
    return v;
}

float luma(vec3 c) {
    return dot(c, vec3(0.299, 0.587, 0.114));
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

    float effectAmt = mask * strength;
    if (effectAmt < 0.01) {
        gl_FragColor = original;
        return;
    }

    vec2 px = 1.0 / RENDERSIZE;

    // Scale effect parameters by mask amount so transition zone gets less effect, not ghosting
    float scaledBleed = bleed * effectAmt;
    float scaledRadius = max(1.0, radius * effectAmt);
    float scaledEdgeDarken = edgeDarken * effectAmt;
    float scaledPaperGrain = paperGrain * effectAmt;
    float scaledSpread = spread * effectAmt;

    // Content-aware flow: compute luminance gradient to find "outward from bright" direction
    float sampleDist = 5.0;
    float lumL = luma(IMG_NORM_PIXEL(inputImage, uv + vec2(-sampleDist, 0.0) * px).rgb);
    float lumR = luma(IMG_NORM_PIXEL(inputImage, uv + vec2( sampleDist, 0.0) * px).rgb);
    float lumU = luma(IMG_NORM_PIXEL(inputImage, uv + vec2(0.0,  sampleDist) * px).rgb);
    float lumD = luma(IMG_NORM_PIXEL(inputImage, uv + vec2(0.0, -sampleDist) * px).rgb);
    // Gradient points dark-to-bright; negate so flow pushes bright content outward
    vec2 lumGrad = -vec2(lumR - lumL, lumU - lumD);
    float gradLen = length(lumGrad);
    vec2 gradDir = (gradLen > 0.001) ? lumGrad / gradLen : vec2(0.0);
    float edgeProximity = smoothstep(0.01, 0.25, gradLen);

    // Animated time offset for organic bleed evolution
    float t = TIME * speed;

    // Multi-scale noise for organic variation - animated
    float n1 = fbm(uv * RENDERSIZE * 0.012 + vec2(t * 0.7, t * -0.5)) * 2.0 - 1.0;
    float n2 = fbm(uv * RENDERSIZE * 0.012 + vec2(t * -0.6, t * 0.8) + 70.0) * 2.0 - 1.0;
    vec2 noiseDir = vec2(n1, n2);

    // Blend content-aware outward flow with organic noise
    // Near edges: mostly push outward; far from edges: mostly noise wander
    vec2 baseFlow = mix(noiseDir, gradDir + noiseDir * 0.4, edgeProximity * 0.7);

    // Chained warp passes - each pass samples noise at the previously warped
    // position, creating long organic tendrils that ripple outward
    vec2 sampleUV = uv;

    // Pass 1
    vec2 flow1 = baseFlow;
    sampleUV += flow1 * scaledBleed * px;

    // Pass 2: re-sample noise at warped position - animated
    float c2a = fbm(sampleUV * RENDERSIZE * 0.02 + vec2(t * 0.5, t * 0.3) + 20.0) * 2.0 - 1.0;
    float c2b = fbm(sampleUV * RENDERSIZE * 0.02 + vec2(t * -0.4, t * 0.6) + 80.0) * 2.0 - 1.0;
    vec2 flow2 = mix(vec2(c2a, c2b), gradDir + vec2(c2a, c2b) * 0.5, edgeProximity * 0.5);
    sampleUV += flow2 * scaledBleed * px * scaledSpread;

    // Pass 3: re-sample again for even longer tendrils - animated
    float c3a = fbm(sampleUV * RENDERSIZE * 0.018 + vec2(t * -0.3, t * 0.4) + 40.0) * 2.0 - 1.0;
    float c3b = fbm(sampleUV * RENDERSIZE * 0.018 + vec2(t * 0.5, t * -0.7) + 110.0) * 2.0 - 1.0;
    vec2 flow3 = mix(vec2(c3a, c3b), gradDir + vec2(c3a, c3b) * 0.6, edgeProximity * 0.4);
    sampleUV += flow3 * scaledBleed * px * scaledSpread * scaledSpread;

    // Kuwahara filter - Gaussian-weighted 4-quadrant approach
    // Divides neighborhood into 4 sectors, calculates weighted mean and variance,
    // then blends sectors by inverse variance. This smooths flat areas into
    // paint-like washes while preserving sharp edges - the key to a painterly look.
    vec3 m0 = vec3(0.0), m1 = vec3(0.0), m2 = vec3(0.0), m3 = vec3(0.0);
    vec3 sq0 = vec3(0.0), sq1 = vec3(0.0), sq2 = vec3(0.0), sq3 = vec3(0.0);
    float w0 = 0.0, w1 = 0.0, w2 = 0.0, w3 = 0.0;

    float R = scaledRadius;
    float R2 = R * R;
    float sigma = R * 0.45;
    float invSigSq2 = 1.0 / (2.0 * sigma * sigma);

    for (int j = -8; j <= 8; j++) {
        for (int i = -8; i <= 8; i++) {
            float fi = float(i);
            float fj = float(j);
            float d2 = fi * fi + fj * fj;
            if (d2 > R2) continue;

            float gw = exp(-d2 * invSigSq2);
            vec3 c = IMG_NORM_PIXEL(inputImage, sampleUV + vec2(fi, fj) * px).rgb;

            if (fi >= 0.0 && fj >= 0.0) { m0 += c * gw; sq0 += c * c * gw; w0 += gw; }
            if (fi <= 0.0 && fj >= 0.0) { m1 += c * gw; sq1 += c * c * gw; w1 += gw; }
            if (fi <= 0.0 && fj <= 0.0) { m2 += c * gw; sq2 += c * c * gw; w2 += gw; }
            if (fi >= 0.0 && fj <= 0.0) { m3 += c * gw; sq3 += c * c * gw; w3 += gw; }
        }
    }

    m0 /= w0; sq0 = sq0 / w0 - m0 * m0;
    m1 /= w1; sq1 = sq1 / w1 - m1 * m1;
    m2 /= w2; sq2 = sq2 / w2 - m2 * m2;
    m3 /= w3; sq3 = sq3 / w3 - m3 * m3;

    float v0 = luma(sq0), v1 = luma(sq1), v2 = luma(sq2), v3 = luma(sq3);

    // High sharpness picks the most uniform quadrant cleanly at edges
    float sharp = 8000.0;
    float bw0 = 1.0 / (1.0 + v0 * sharp);
    float bw1 = 1.0 / (1.0 + v1 * sharp);
    float bw2 = 1.0 / (1.0 + v2 * sharp);
    float bw3 = 1.0 / (1.0 + v3 * sharp);
    float bwSum = bw0 + bw1 + bw2 + bw3;
    vec3 col = (m0 * bw0 + m1 * bw1 + m2 * bw2 + m3 * bw3) / bwSum;

    // Sobel edge detection for pigment accumulation darkening - sample at displaced position
    vec3 tl = IMG_NORM_PIXEL(inputImage, sampleUV + vec2(-1.0, 1.0) * px * 2.0).rgb;
    vec3 tc = IMG_NORM_PIXEL(inputImage, sampleUV + vec2( 0.0, 1.0) * px * 2.0).rgb;
    vec3 tr = IMG_NORM_PIXEL(inputImage, sampleUV + vec2( 1.0, 1.0) * px * 2.0).rgb;
    vec3 ml = IMG_NORM_PIXEL(inputImage, sampleUV + vec2(-1.0, 0.0) * px * 2.0).rgb;
    vec3 mr = IMG_NORM_PIXEL(inputImage, sampleUV + vec2( 1.0, 0.0) * px * 2.0).rgb;
    vec3 bl = IMG_NORM_PIXEL(inputImage, sampleUV + vec2(-1.0,-1.0) * px * 2.0).rgb;
    vec3 bc = IMG_NORM_PIXEL(inputImage, sampleUV + vec2( 0.0,-1.0) * px * 2.0).rgb;
    vec3 br = IMG_NORM_PIXEL(inputImage, sampleUV + vec2( 1.0,-1.0) * px * 2.0).rgb;

    vec3 gx = -tl - 2.0 * ml - bl + tr + 2.0 * mr + br;
    vec3 gy = -tl - 2.0 * tc - tr + bl + 2.0 * bc + br;
    float edge = length(gx) + length(gy);
    edge = smoothstep(0.08, 0.6, edge);
    // Warm-tinted darkening mimics pigment settling at wet edges
    col -= edge * scaledEdgeDarken * vec3(0.18, 0.15, 0.12);

    // Paper grain - more visible in lighter areas (paint sits in paper texture)
    float grain = noise(uv * RENDERSIZE * 0.4) * 2.0 - 1.0;
    grain += noise(uv * RENDERSIZE * 1.2) * 0.4;
    float grainMask = 1.0 - smoothstep(0.3, 0.9, luma(col));
    col += grain * scaledPaperGrain * grainMask * 0.2;

    // blendMask: old behavior (mix with original, can ghost). Off: no ghosting.
    if (blendMask) {
        gl_FragColor = vec4(mix(original.rgb, col, effectAmt), original.a);
    } else {
        gl_FragColor = vec4(col, original.a);
    }
}
