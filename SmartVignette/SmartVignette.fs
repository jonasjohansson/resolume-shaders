/*{
    "DESCRIPTION": "Vignette with round/square modes, independent width/height, movable center, and optional image mask with inner shadow",
    "CREDIT": "Jonas",
    "ISFVSN": "2",
    "CATEGORIES": ["Stylize"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "maskImage", "TYPE": "image", "FILE": true },
        { "NAME": "shape", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "centerX", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "centerY", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "sizeX", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "sizeY", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.001, "MAX": 1.0, "DEFAULT": 0.1 },
        { "NAME": "roundness", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "imageMask", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "softness", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 color = IMG_NORM_PIXEL(inputImage, uv);

    float aspect = RENDERSIZE.x / RENDERSIZE.y;

    // --- Geometric vignette ---
    vec2 center = vec2(centerX, centerY);
    vec2 d = uv - center;
    d.x *= aspect;

    float geo = 0.0;

    if (!shape) {
        vec2 radii = vec2(sizeX * aspect, sizeY);
        vec2 safeRadii = max(radii, vec2(0.001));
        float ellipse = length(d / safeRadii);
        geo = smoothstep(1.0, 1.0 - fadeWidth, ellipse);
    } else {
        vec2 halfSize = vec2(sizeX * aspect, sizeY);
        vec2 safeHalf = max(halfSize, vec2(0.001));
        float r = roundness * min(safeHalf.x, safeHalf.y);
        vec2 q = abs(d) - safeHalf + vec2(r);
        float sdf = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
        geo = smoothstep(0.0, -fadeWidth * min(safeHalf.x, safeHalf.y), sdf);
    }

    // --- Image mask with pre-computed distance field ---
    // R = mask value, G = normalized distance from edge (computed on CPU at load time)
    vec4 maskData = IMG_NORM_PIXEL(maskImage, uv);
    float sharpMask = maskData.r;
    float dist = maskData.g;
    float imgMask = sharpMask * smoothstep(0.0, softness, dist);

    // Blend geometric and image mask
    float mask = mix(geo, imgMask, imageMask);

    if (invert) {
        mask = 1.0 - mask;
    }

    color.a *= mask;

    gl_FragColor = color;
}
