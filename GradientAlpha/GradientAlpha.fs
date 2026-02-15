/*{
    "DESCRIPTION": "Gradient alpha mask - control layer opacity with a gradient",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.2 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.2 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "shape", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 col = IMG_NORM_PIXEL(inputImage, uv);

    float mask;

    if (!shape) {
        // Linear gradient
        float aspect = RENDERSIZE.x / RENDERSIZE.y;
        vec2 center = vec2(0.5, 0.5);
        vec2 p = uv - center;

        // Rotate the axis
        float cosA = cos(angle);
        float sinA = sin(angle);
        float axis = (p.x * cosA + p.y * sinA) + 0.5;

        float dist = abs(axis - maskPos);
        mask = smoothstep(maskWidth, maskWidth + fadeWidth, dist);
    } else {
        // Radial gradient
        vec2 center = vec2(maskPos, 0.5);
        vec2 p = uv - center;
        float aspect = RENDERSIZE.x / RENDERSIZE.y;
        p.x *= aspect;
        float dist = length(p);
        mask = smoothstep(maskWidth, maskWidth + fadeWidth, dist);
    }

    if (invert) mask = 1.0 - mask;

    col.a *= mask;
    gl_FragColor = col;
}
