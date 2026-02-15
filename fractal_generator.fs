/*{
    "DESCRIPTION": "Fractal generator with Mandelbrot and Julia set modes",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        { "NAME": "mode", "TYPE": "long", "VALUES": [0, 1], "LABELS": ["Mandelbrot", "Julia"], "DEFAULT": 0 },
        { "NAME": "iterations", "TYPE": "float", "MIN": 10.0, "MAX": 300.0, "DEFAULT": 100.0 },
        { "NAME": "zoom", "TYPE": "float", "MIN": 0.1, "MAX": 50.0, "DEFAULT": 1.0 },
        { "NAME": "panX", "TYPE": "float", "MIN": -3.0, "MAX": 3.0, "DEFAULT": -0.5 },
        { "NAME": "panY", "TYPE": "float", "MIN": -3.0, "MAX": 3.0, "DEFAULT": 0.0 },
        { "NAME": "juliaReal", "TYPE": "float", "MIN": -2.0, "MAX": 2.0, "DEFAULT": -0.7269 },
        { "NAME": "juliaImag", "TYPE": "float", "MIN": -2.0, "MAX": 2.0, "DEFAULT": 0.1889 },
        { "NAME": "colorSpeed", "TYPE": "float", "MIN": 0.1, "MAX": 10.0, "DEFAULT": 1.0 },
        { "NAME": "colorOffset", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "animate", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "animateSpeed", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.1 }
    ]
}*/

vec3 palette(float t) {
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.0, 0.33, 0.67);
    return a + b * cos(6.28318 * (c * t + d));
}

void main() {
    vec2 uv = isf_FragNormCoord;
    uv = (uv - 0.5) * 2.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;

    uv /= zoom;
    uv += vec2(panX, panY);

    vec2 c;
    vec2 z;

    if (mode == 0) {
        c = uv;
        z = vec2(0.0);
    } else {
        z = uv;
        if (animate) {
            c = vec2(
                juliaReal + sin(TIME * animateSpeed) * 0.2,
                juliaImag + cos(TIME * animateSpeed * 0.7) * 0.2
            );
        } else {
            c = vec2(juliaReal, juliaImag);
        }
    }

    int maxIter = int(iterations);
    float iter = 0.0;

    for (int i = 0; i < 300; i++) {
        if (i >= maxIter) break;
        if (dot(z, z) > 4.0) break;
        z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
        iter += 1.0;
    }

    if (iter >= float(maxIter)) {
        gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
    } else {
        float smooth_iter = iter - log2(log2(dot(z, z))) + 4.0;
        float t = smooth_iter * colorSpeed * 0.02 + colorOffset;
        if (animate && mode == 0) {
            t += TIME * animateSpeed * 0.1;
        }
        vec3 col = palette(t);
        gl_FragColor = vec4(col, 1.0);
    }
}
