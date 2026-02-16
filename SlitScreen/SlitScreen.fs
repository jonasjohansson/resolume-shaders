/*{
    "DESCRIPTION": "Slit screen - repeats boundary pixels outward from a gradient mask edge",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "amount", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.01, "MAX": 0.5, "DEFAULT": 0.05 },
        { "NAME": "angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

void main() {
    vec2 uv = isf_FragNormCoord;

    if (amount < 0.001) {
        gl_FragColor = IMG_NORM_PIXEL(inputImage, uv);
        return;
    }

    vec2 sampleUV = uv;

    if (!radial) {
        float cosA = cos(angle);
        float sinA = sin(angle);
        vec2 p = uv - 0.5;

        // Decompose UV into axis (along mask direction) and perpendicular components
        float axisVal = p.x * cosA + p.y * sinA;
        float perpVal = -p.x * sinA + p.y * cosA;

        // Distance from mask center along the axis
        float maskCenter = maskPos - 0.5;
        float dist = axisVal - maskCenter;

        // Clamp axis to stay within the maskWidth band
        float clampedDist = clamp(dist, -maskWidth, maskWidth);
        float clampedAxis = maskCenter + clampedDist;

        // Reconstruct UV with clamped axis but original perpendicular
        vec2 clampedP = clampedAxis * vec2(cosA, sinA) + perpVal * vec2(-sinA, cosA);
        vec2 clampedUV = clampedP + 0.5;

        // How far outside the band we are determines blend strength
        float overshoot = max(abs(dist) - maskWidth, 0.0);
        float effectStr = smoothstep(0.0, fadeWidth, overshoot);
        if (invert) effectStr = 1.0 - effectStr;

        sampleUV = mix(uv, clampedUV, effectStr * amount);
    } else {
        // Radial: clamp distance from center to maskWidth radius
        vec2 center = vec2(maskPos, 0.5);
        vec2 diff = uv - center;
        diff.x *= RENDERSIZE.x / RENDERSIZE.y;
        float dist = length(diff);

        if (dist > maskWidth && dist > 0.001) {
            vec2 dir = diff / dist;
            vec2 clampedDiff = dir * maskWidth;
            clampedDiff.x /= RENDERSIZE.x / RENDERSIZE.y;
            vec2 clampedUV = center + clampedDiff;

            float overshoot = dist - maskWidth;
            float effectStr = smoothstep(0.0, fadeWidth, overshoot);
            if (invert) effectStr = 1.0 - effectStr;

            sampleUV = mix(uv, clampedUV, effectStr * amount);
        }
    }

    gl_FragColor = IMG_NORM_PIXEL(inputImage, sampleUV);
}
