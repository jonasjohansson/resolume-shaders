/*{
    "DESCRIPTION": "Gaussian blur with gradient mask control",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "amount", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "blurSize", "TYPE": "float", "MIN": 0.0, "MAX": 60.0, "DEFAULT": 10.0 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.2 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "blendMask", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

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
    if (effectAmt < 0.01) {
        gl_FragColor = original;
        return;
    }

    vec2 px = 1.0 / RENDERSIZE;

    // Adaptive radius: blur strength scales with mask
    float r = blurSize * effectAmt;

    // Gaussian blur with 31x31 kernel
    vec3 blurred = vec3(0.0);
    float totalWeight = 0.0;
    float sigma = r * 0.5;
    float invSig2 = 1.0 / (2.0 * sigma * sigma + 0.001);

    for (int j = -15; j <= 15; j++) {
        for (int i = -15; i <= 15; i++) {
            float fi = float(i);
            float fj = float(j);
            float d2 = fi * fi + fj * fj;
            if (d2 > 225.0) continue;
            float w = exp(-d2 * invSig2);
            blurred += IMG_NORM_PIXEL(inputImage, uv + vec2(fi, fj) * px * r * 0.07).rgb * w;
            totalWeight += w;
        }
    }
    blurred /= totalWeight;

    if (blendMask) {
        gl_FragColor = vec4(mix(original.rgb, blurred, effectAmt), original.a);
    } else {
        gl_FragColor = vec4(blurred, original.a);
    }
}
