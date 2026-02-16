/*{
    "DESCRIPTION": "Concentric rings that ripple outward from edges of source shapes",
    "CREDIT": "Jonas",
    "ISFVSN": "2",
    "CATEGORIES": ["Stylize"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "frequency", "TYPE": "float", "MIN": 1.0, "MAX": 50.0, "DEFAULT": 15.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 5.0, "DEFAULT": 1.5 },
        { "NAME": "reach", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "thickness", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "decay", "TYPE": "float", "MIN": 0.1, "MAX": 5.0, "DEFAULT": 1.5 },
        { "NAME": "wobble", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 }
    ]
}*/

vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453);
}

float gnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(dot(hash22(i), f),
                   dot(hash22(i + vec2(1,0)), f - vec2(1,0)), u.x),
               mix(dot(hash22(i + vec2(0,1)), f - vec2(0,1)),
                   dot(hash22(i + vec2(1,1)), f - vec2(1,1)), u.x), u.y);
}

float fbm(vec2 p) {
    float v = 0.0, a = 0.5;
    mat2 rot = mat2(0.877, 0.479, -0.479, 0.877);
    for (int i = 0; i < 5; i++) {
        v += a * gnoise(p);
        p = rot * p * 2.0 + vec2(100.0);
        a *= 0.5;
    }
    return v;
}

float luma(vec3 c) {
    return dot(c, vec3(0.299, 0.587, 0.114));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = RENDERSIZE.x / RENDERSIZE.y;
    float srcLum = luma(IMG_NORM_PIXEL(inputImage, uv).rgb);

    // Search radius in UV space — generous to cover distant shapes
    float maxReach = reach * 0.5;

    // Find distance to nearest white pixel (Fibonacci disc, 48 samples)
    float minR = maxReach;
    float jitter = fract(sin(dot(uv * RENDERSIZE, vec2(12.9898, 78.233))) * 43758.5453) * 6.28318;

    for (int i = 1; i <= 48; i++) {
        float r = sqrt(float(i) / 48.0) * maxReach;
        float angle = float(i) * 2.39996 + jitter;
        vec2 offset = vec2(cos(angle), sin(angle)) * r;
        offset.x /= aspect;
        float s = luma(IMG_NORM_PIXEL(inputImage, uv + offset).rgb);
        if (s > 0.2) {
            minR = min(minR, r);
        }
    }

    // Also check the pixel itself
    if (srcLum > 0.2) {
        minR = 0.0;
    }

    // Normalized distance (0 = on shape, 1 = at max reach)
    float normDist = minR / maxReach;

    // Nothing found within reach
    if (normDist >= 1.0) {
        gl_FragColor = vec4(vec3(srcLum), 1.0);
        return;
    }

    // Add noise wobble for organic ring shapes
    float noiseWobble = fbm(uv * 6.0 + TIME * speed * 0.05) * wobble * maxReach * 0.3;
    float dist = minR + noiseWobble;

    // Expanding ring pattern
    float phase = dist * frequency * 6.28318 / maxReach - TIME * speed * 4.0;
    float ring = pow(max(0.5 + 0.5 * cos(phase), 0.0), 1.0 / max(thickness, 0.01));

    // Gradual decay with distance
    ring *= 1.0 - pow(normDist, 1.0 / max(decay, 0.1));

    // Suppress rings directly on top of source shapes
    ring *= smoothstep(0.0, 0.05, normDist);

    float result = max(srcLum, ring);
    gl_FragColor = vec4(vec3(result), 1.0);
}
