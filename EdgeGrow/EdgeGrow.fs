/*{
    "DESCRIPTION": "Organic lichen-like growth extending from edges of source shapes",
    "CREDIT": "Jonas",
    "ISFVSN": "2",
    "CATEGORIES": ["Stylize"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "growth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "detail", "TYPE": "float", "MIN": 1.0, "MAX": 20.0, "DEFAULT": 6.0 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.15 },
        { "NAME": "intensity", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.8 }
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
    for (int i = 0; i < 6; i++) {
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
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    float srcLum = luma(src.rgb);

    float t = TIME * speed;
    float maxReach = growth * 0.15;

    // Find approximate distance to nearest white pixel (Fibonacci disc)
    float minDist = 1.0;
    float jitter = fract(sin(dot(uv * RENDERSIZE, vec2(12.9898, 78.233))) * 43758.5453) * 6.28318;

    for (int i = 1; i <= 32; i++) {
        float r = sqrt(float(i) / 32.0) * maxReach;
        float angle = float(i) * 2.39996 + jitter;
        vec2 offset = vec2(cos(angle), sin(angle)) * r;
        offset.x /= aspect;
        float s = luma(IMG_NORM_PIXEL(inputImage, uv + offset).rgb);
        if (s > 0.3) {
            minDist = min(minDist, r / maxReach);
        }
    }

    // Two-level domain warping for organic branching structure
    vec2 np = uv * detail;
    float w1x = fbm(np + t * 0.13);
    float w1y = fbm(np + vec2(5.2, 1.3) + t * 0.11);
    vec2 warped = np + vec2(w1x, w1y) * 1.5;
    float w2x = fbm(warped + vec2(1.7, 9.2) + t * 0.07);
    float w2y = fbm(warped + vec2(8.3, 2.8) + t * 0.09);
    float growthField = fbm(warped + vec2(w2x, w2y) * 1.5);

    // Growth threshold: easier to grow near edges, harder far away
    float proximity = 1.0 - minDist;
    float threshold = smoothstep(0.0, 1.0, proximity);
    float grown = smoothstep(0.3 - threshold * 0.35, 0.3, growthField);

    // Fine detail layer for tendril-like edges
    float fineDetail = fbm(uv * detail * 4.0 + t * 0.2);
    grown *= smoothstep(0.15, 0.5, fineDetail);

    // Fade with distance from source
    grown *= proximity * intensity;

    // Only grow where there's actually a nearby source
    grown *= step(0.001, 1.0 - minDist);

    float result = max(srcLum, grown);
    gl_FragColor = vec4(vec3(result), 1.0);
}
