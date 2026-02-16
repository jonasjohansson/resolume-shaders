/*{
    "DESCRIPTION": "Ethereal ghost copies drift from source shapes like traces of presence",
    "CREDIT": "Jonas",
    "ISFVSN": "2",
    "CATEGORIES": ["Stylize"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "drift", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "ghosts", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.6 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.2 },
        { "NAME": "scale", "TYPE": "float", "MIN": 1.0, "MAX": 15.0, "DEFAULT": 3.0 },
        { "NAME": "fade", "TYPE": "float", "MIN": 0.1, "MAX": 1.0, "DEFAULT": 0.6 }
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

    float t = TIME * speed;
    float maxDrift = drift * 0.08;
    float result = srcLum;

    // 7 ghost layers, each drifting in a unique noise-driven direction
    for (int i = 0; i < 7; i++) {
        float layer = float(i + 1);
        float seed = layer * 7.13;

        // Each ghost has its own slowly-evolving displacement field
        vec2 np = uv * scale + vec2(seed);
        float dx = fbm(np + vec2(t * (0.5 + layer * 0.1), t * 0.3));
        float dy = fbm(np + vec2(3.7, 8.1) + vec2(t * 0.2, t * (0.4 + layer * 0.08)));

        vec2 displacement = vec2(dx, dy) * maxDrift * layer;
        displacement.x /= aspect;

        float ghost = luma(IMG_NORM_PIXEL(inputImage, uv + displacement).rgb);

        // Progressive fade: each successive ghost is fainter
        float layerFade = pow(fade, layer);
        ghost *= layerFade * ghosts;

        result = max(result, ghost);
    }

    gl_FragColor = vec4(vec3(result), 1.0);
}
