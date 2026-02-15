/*{
    "DESCRIPTION": "Domain-warped FBM - organically warps source content with animated noise",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "amount", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "noiseScale", "TYPE": "float", "MIN": 0.5, "MAX": 10.0, "DEFAULT": 3.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.4 },
        { "NAME": "warpStr", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 0.5, "DEFAULT": 0.2 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

float rand(vec2 n) {
    return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 ip = floor(p);
    vec2 u = fract(p);
    u = u * u * (3.0 - 2.0 * u);
    float res = mix(
        mix(rand(ip), rand(ip + vec2(1.0, 0.0)), u.x),
        mix(rand(ip + vec2(0.0, 1.0)), rand(ip + vec2(1.0, 1.0)), u.x), u.y);
    return res * res;
}

float fbm(vec2 p, float t) {
    mat2 mtx = mat2(0.80, 0.60, -0.60, 0.80);
    float f = 0.0;
    f += 0.500000 * noise(p + t); p = mtx * p * 2.02;
    f += 0.031250 * noise(p); p = mtx * p * 2.01;
    f += 0.250000 * noise(p); p = mtx * p * 2.03;
    f += 0.125000 * noise(p); p = mtx * p * 2.01;
    f += 0.062500 * noise(p); p = mtx * p * 2.04;
    f += 0.015625 * noise(p + sin(t));
    return f / 0.96875;
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

    float t = TIME * speed;

    // Triple domain-warped FBM for complex organic motion
    vec2 noiseUV = uv * noiseScale;
    noiseUV.x *= RENDERSIZE.x / RENDERSIZE.y;

    float f1 = fbm(noiseUV, t);
    float f2 = fbm(noiseUV + f1 * 2.0 + 3.7, t);
    float f3 = fbm(noiseUV + f2 * 2.0 + 9.2, t);

    // Warp source UV with noise displacement - interpolate UV, sample once
    vec2 warp = vec2(f2 - 0.5, f3 - 0.5);
    vec2 warpedUV = uv + warp * warpStr * effectAmt * 0.2;

    gl_FragColor = IMG_NORM_PIXEL(inputImage, warpedUV);
}
