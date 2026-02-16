/*{
    "DESCRIPTION": "Energy pulse that expands and illuminates shapes as it sweeps through them",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        { "NAME": "inputImage", "TYPE": "image" },
        { "NAME": "pulseSpeed", "TYPE": "float", "MIN": 0.0, "MAX": 3.0, "DEFAULT": 0.5 },
        { "NAME": "pulsePos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "pulseWidth", "TYPE": "float", "MIN": 0.01, "MAX": 0.5, "DEFAULT": 0.12 },
        { "NAME": "expandAmount", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.4 },
        { "NAME": "glowIntensity", "TYPE": "float", "MIN": 0.0, "MAX": 5.0, "DEFAULT": 2.0 },
        { "NAME": "chaos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "trackWidth", "TYPE": "float", "MIN": 0.05, "MAX": 1.0, "DEFAULT": 1.0 },
        { "NAME": "pixelate", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "pixelSize", "TYPE": "float", "MIN": 0.005, "MAX": 0.1, "DEFAULT": 0.03 },
        { "NAME": "numPulses", "TYPE": "long", "VALUES": [1, 2, 3, 4, 5, 6, 7], "LABELS": ["1", "2", "3", "4", "5", "6", "7"], "DEFAULT": 1 },
        { "NAME": "trackPos1", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "trackDelay1", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "trackPos2", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.17 },
        { "NAME": "trackDelay2", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.33 },
        { "NAME": "trackPos3", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.83 },
        { "NAME": "trackDelay3", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.67 },
        { "NAME": "trackPos4", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.33 },
        { "NAME": "trackDelay4", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.15 },
        { "NAME": "trackPos5", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.67 },
        { "NAME": "trackDelay5", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "trackPos6", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.1 },
        { "NAME": "trackDelay6", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.85 },
        { "NAME": "trackPos7", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.9 },
        { "NAME": "trackDelay7", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.45 },
        { "NAME": "direction", "TYPE": "long", "VALUES": [0, 1, 2, 3], "LABELS": ["Left to Right", "Right to Left", "Bottom to Top", "Top to Bottom"], "DEFAULT": 0 },
        { "NAME": "maskPos", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "maskWidth", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 1.0 },
        { "NAME": "fadeWidth", "TYPE": "float", "MIN": 0.0, "MAX": 0.5, "DEFAULT": 0.0 },
        { "NAME": "angle", "TYPE": "float", "MIN": -1.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "radial", "TYPE": "bool", "DEFAULT": false },
        { "NAME": "invert", "TYPE": "bool", "DEFAULT": false }
    ]
}*/

float hash11(float p) {
    p = fract(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return fract(p);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 original = IMG_NORM_PIXEL(inputImage, uv);

    // Gradient mask
    float maskA = angle * 3.14159265;
    float maskVal;
    if (radial) {
        vec2 centered = uv - 0.5;
        centered.x *= RENDERSIZE.x / RENDERSIZE.y;
        maskVal = length(centered) * 2.0;
    } else {
        vec2 centered = uv - 0.5;
        maskVal = (centered.x * cos(maskA) + centered.y * sin(maskA)) + 0.5;
    }
    float maskStart = maskPos;
    float maskEnd = maskPos + maskWidth;
    float mask = smoothstep(maskStart - fadeWidth, maskStart + fadeWidth * 0.1, maskVal)
               * (1.0 - smoothstep(maskEnd - fadeWidth * 0.1, maskEnd + fadeWidth, maskVal));
    if (invert) mask = 1.0 - mask;

    // Travel axis and perpendicular axis
    float axisVal = uv.x;
    float perpVal = uv.y;
    if (direction == 1) { axisVal = 1.0 - uv.x; perpVal = uv.y; }
    if (direction == 2) { axisVal = uv.y; perpVal = uv.x; }
    if (direction == 3) { axisVal = 1.0 - uv.y; perpVal = uv.x; }

    float margin = pulseWidth * 1.5 + expandAmount * 0.3;
    float totalPulse = 0.0;
    float closestDist = 1.0;

    // === Pulse 1 (always active) ===
    float tP1 = TIME * pulseSpeed * 0.25 + trackDelay1 + pulsePos;
    float cyc1 = floor(tP1);
    float ph1 = fract(tP1) + (hash11(cyc1 * 7.3) * 2.0 - 1.0) * chaos * 0.15;
    float sw1 = mix(-margin, 1.0 + margin, clamp(ph1 / 0.8, 0.0, 1.0));
    float trk1 = trackPos1 + (hash11(cyc1 * 11.7) - 0.5) * chaos * 0.15;
    float pw1 = smoothstep(trackWidth, 0.0, abs(perpVal - trk1));
    float dP1 = abs(axisVal - sw1);
    float pp1 = smoothstep(pulseWidth, 0.0, dP1);
    pp1 = pp1 * pp1 * pw1;
    totalPulse = max(totalPulse, pp1);
    if (pp1 > 0.01) closestDist = min(closestDist, dP1);

    // === Pulse 2 ===
    if (numPulses >= 2) {
        float tP2 = TIME * pulseSpeed * 0.25 + trackDelay2 + pulsePos;
        float cyc2 = floor(tP2);
        float ph2 = fract(tP2) + (hash11(cyc2 * 13.1) * 2.0 - 1.0) * chaos * 0.15;
        float sw2 = mix(-margin, 1.0 + margin, clamp(ph2 / 0.8, 0.0, 1.0));
        float trk2 = trackPos2 + (hash11(cyc2 * 17.3) - 0.5) * chaos * 0.15;
        float pw2 = smoothstep(trackWidth, 0.0, abs(perpVal - trk2));
        float dP2 = abs(axisVal - sw2);
        float pp2 = smoothstep(pulseWidth, 0.0, dP2);
        pp2 = pp2 * pp2 * pw2;
        totalPulse = max(totalPulse, pp2);
        if (pp2 > 0.01) closestDist = min(closestDist, dP2);
    }

    // === Pulse 3 ===
    if (numPulses >= 3) {
        float tP3 = TIME * pulseSpeed * 0.25 + trackDelay3 + pulsePos;
        float cyc3 = floor(tP3);
        float ph3 = fract(tP3) + (hash11(cyc3 * 19.7) * 2.0 - 1.0) * chaos * 0.15;
        float sw3 = mix(-margin, 1.0 + margin, clamp(ph3 / 0.8, 0.0, 1.0));
        float trk3 = trackPos3 + (hash11(cyc3 * 23.1) - 0.5) * chaos * 0.15;
        float pw3 = smoothstep(trackWidth, 0.0, abs(perpVal - trk3));
        float dP3 = abs(axisVal - sw3);
        float pp3 = smoothstep(pulseWidth, 0.0, dP3);
        pp3 = pp3 * pp3 * pw3;
        totalPulse = max(totalPulse, pp3);
        if (pp3 > 0.01) closestDist = min(closestDist, dP3);
    }

    // === Pulse 4 ===
    if (numPulses >= 4) {
        float tP4 = TIME * pulseSpeed * 0.25 + trackDelay4 + pulsePos;
        float cyc4 = floor(tP4);
        float ph4 = fract(tP4) + (hash11(cyc4 * 29.3) * 2.0 - 1.0) * chaos * 0.15;
        float sw4 = mix(-margin, 1.0 + margin, clamp(ph4 / 0.8, 0.0, 1.0));
        float trk4 = trackPos4 + (hash11(cyc4 * 31.7) - 0.5) * chaos * 0.15;
        float pw4 = smoothstep(trackWidth, 0.0, abs(perpVal - trk4));
        float dP4 = abs(axisVal - sw4);
        float pp4 = smoothstep(pulseWidth, 0.0, dP4);
        pp4 = pp4 * pp4 * pw4;
        totalPulse = max(totalPulse, pp4);
        if (pp4 > 0.01) closestDist = min(closestDist, dP4);
    }

    // === Pulse 5 ===
    if (numPulses >= 5) {
        float tP5 = TIME * pulseSpeed * 0.25 + trackDelay5 + pulsePos;
        float cyc5 = floor(tP5);
        float ph5 = fract(tP5) + (hash11(cyc5 * 37.1) * 2.0 - 1.0) * chaos * 0.15;
        float sw5 = mix(-margin, 1.0 + margin, clamp(ph5 / 0.8, 0.0, 1.0));
        float trk5 = trackPos5 + (hash11(cyc5 * 41.3) - 0.5) * chaos * 0.15;
        float pw5 = smoothstep(trackWidth, 0.0, abs(perpVal - trk5));
        float dP5 = abs(axisVal - sw5);
        float pp5 = smoothstep(pulseWidth, 0.0, dP5);
        pp5 = pp5 * pp5 * pw5;
        totalPulse = max(totalPulse, pp5);
        if (pp5 > 0.01) closestDist = min(closestDist, dP5);
    }

    // === Pulse 6 ===
    if (numPulses >= 6) {
        float tP6 = TIME * pulseSpeed * 0.25 + trackDelay6 + pulsePos;
        float cyc6 = floor(tP6);
        float ph6 = fract(tP6) + (hash11(cyc6 * 43.7) * 2.0 - 1.0) * chaos * 0.15;
        float sw6 = mix(-margin, 1.0 + margin, clamp(ph6 / 0.8, 0.0, 1.0));
        float trk6 = trackPos6 + (hash11(cyc6 * 47.1) - 0.5) * chaos * 0.15;
        float pw6 = smoothstep(trackWidth, 0.0, abs(perpVal - trk6));
        float dP6 = abs(axisVal - sw6);
        float pp6 = smoothstep(pulseWidth, 0.0, dP6);
        pp6 = pp6 * pp6 * pw6;
        totalPulse = max(totalPulse, pp6);
        if (pp6 > 0.01) closestDist = min(closestDist, dP6);
    }

    // === Pulse 7 ===
    if (numPulses >= 7) {
        float tP7 = TIME * pulseSpeed * 0.25 + trackDelay7 + pulsePos;
        float cyc7 = floor(tP7);
        float ph7 = fract(tP7) + (hash11(cyc7 * 53.3) * 2.0 - 1.0) * chaos * 0.15;
        float sw7 = mix(-margin, 1.0 + margin, clamp(ph7 / 0.8, 0.0, 1.0));
        float trk7 = trackPos7 + (hash11(cyc7 * 59.7) - 0.5) * chaos * 0.15;
        float pw7 = smoothstep(trackWidth, 0.0, abs(perpVal - trk7));
        float dP7 = abs(axisVal - sw7);
        float pp7 = smoothstep(pulseWidth, 0.0, dP7);
        pp7 = pp7 * pp7 * pw7;
        totalPulse = max(totalPulse, pp7);
        if (pp7 > 0.01) closestDist = min(closestDist, dP7);
    }

    float effectStrength = totalPulse * mask;

    if (effectStrength < 0.001) {
        gl_FragColor = original;
        return;
    }

    // === Pixelate at pulse position ===
    vec2 sampleUV = uv;
    if (pixelate) {
        float pxSize = pixelSize * effectStrength;
        if (pxSize > 0.002) {
            sampleUV = floor(uv / pxSize) * pxSize + pxSize * 0.5;
        }
    }

    vec4 baseColor = IMG_NORM_PIXEL(inputImage, sampleUV);

    // === Dilation: hard max for sharp expansion ===
    float radiusPixels = expandAmount * 120.0 * effectStrength;

    vec4 dilated = baseColor;

    if (radiusPixels > 0.5) {
        float angleOffset = fract(sin(dot(sampleUV * RENDERSIZE, vec2(12.9898, 78.233))) * 43758.5453) * 6.2831853;

        for (int ai = 0; ai < 24; ai++) {
            float ang = float(ai) * 6.2831853 / 24.0 + angleOffset;
            vec2 dir = vec2(cos(ang), sin(ang));
            for (int sj = 1; sj <= 6; sj++) {
                float r = radiusPixels * float(sj) / 6.0;
                vec2 tapUV = sampleUV + dir * r / RENDERSIZE;
                vec4 s = IMG_NORM_PIXEL(inputImage, tapUV);
                dilated = max(dilated, s);
            }
        }
    }

    // === Glow ===
    float brightness = dot(dilated.rgb, vec3(0.299, 0.587, 0.114));
    vec3 glowed = dilated.rgb * (1.0 + glowIntensity * effectStrength * (0.5 + brightness));

    // Hot core
    float corePulse = smoothstep(pulseWidth * 0.4, 0.0, closestDist);
    corePulse = corePulse * corePulse * corePulse;
    glowed += glowed * corePulse * glowIntensity * 0.2 * mask;

    gl_FragColor = vec4(mix(original.rgb, glowed, effectStrength), original.a);
}
