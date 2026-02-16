/*{
    "DESCRIPTION": "Graphic score generator - 3 tracks of organic shapes",
    "CREDIT": "Jonas Johansson",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        { "NAME": "track1", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.7 },
        { "NAME": "track2", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.7 },
        { "NAME": "track3", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.7 },
        { "NAME": "shapeSize", "TYPE": "float", "MIN": 0.1, "MAX": 2.0, "DEFAULT": 0.5 },
        { "NAME": "spread", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "scale", "TYPE": "float", "MIN": 0.2, "MAX": 6.0, "DEFAULT": 1.0 },
        { "NAME": "density", "TYPE": "float", "MIN": 0.1, "MAX": 1.0, "DEFAULT": 0.4 },
        { "NAME": "speed", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "scrollSpeed", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "scrollAngle", "TYPE": "float", "MIN": 0.0, "MAX": 6.28, "DEFAULT": 6.28 },
        { "NAME": "complexity", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
        { "NAME": "softness", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.0 },
        { "NAME": "lineThickness", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.3 },
        { "NAME": "seed", "TYPE": "float", "MIN": 0.0, "MAX": 100.0, "DEFAULT": 0.0 }
    ]
}*/

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec2 hash2(vec2 p) {
    return vec2(hash(p), hash(p + 127.1));
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a, ba = b - a;
    float denom = dot(ba, ba);
    if (denom < 0.0001) return length(pa);
    float h = clamp(dot(pa, ba) / denom, 0.0, 1.0);
    return length(pa - ba * h);
}

float sdWobblyCircle(vec2 p, vec2 center, float r, float wobble, float wseed) {
    vec2 d = p - center;
    float ang = atan(d.y, d.x);
    float deform = noise(vec2(ang * 3.0 + wseed, wseed * 7.0)) * wobble
                 + noise(vec2(ang * 7.0 + wseed * 3.0, wseed * 13.0)) * wobble * 0.5;
    return length(d) - r * (1.0 + deform);
}

float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    vec2 clamped = vec2(max(d.x, 0.0), max(d.y, 0.0));
    return length(clamped) + min(max(d.x, d.y), 0.0);
}

// Generate one track of shapes at a given y center
float generateTrack(vec2 uv, float trackY, float t, float trackSeed, float amt) {
    if (amt < 0.01) return 0.0;

    float soft = 0.002 + softness * 0.008;
    float result = 0.0;
    float sz = shapeSize;

    // Early out if pixel is too far from track to possibly hit any shape
    float maxReach = 0.08 * sz + spread * 0.06;
    float trackDist = abs(uv.y - trackY);
    if (trackDist > maxReach) return 0.0;

    // How tightly shapes cluster around the line (0 = on line, 1 = wider spread)
    float ySpread = 0.01 + spread * 0.05;

    // Mode cycling: spatial zones along x-axis, each track offset
    // Shapes scroll in from the right, so composition changes arrive naturally
    float phase = uv.x * 0.15 + trackSeed * 0.25;
    float mv1 = max(0.0, sin(phase * 6.28));          // Delicate: dots, nodes, leaves
    float mv2 = max(0.0, sin(phase * 6.28 + 1.57));   // Sweeping: long strokes
    float mv3 = max(0.0, sin(phase * 6.28 + 3.14));   // Bold: blobs, drips
    float mv4 = max(0.0, sin(phase * 6.28 + 4.71));   // Textural: hatching, rects

    // Shape type weights - base level + movement contribution
    float wDots    = 0.15 + 0.85 * mv1;
    float wNodes   = 0.15 + 0.85 * mv1;
    float wStrokes = 0.2  + 0.8  * mv2;
    float wBlobs   = 0.1  + 0.7  * mv3;
    float wDrips   = 0.1  + 0.9  * mv3;
    float wHatch   = 0.1  + 0.9  * mv4;
    float wLeaves  = 0.1  + 0.9  * mv4;
    float wRects   = 0.1  + 0.9  * mv4;

    // === 1. WOBBLY DOTS + SPLATTERS ===
    for (int layer = 0; layer < 2; layer++) {
        float fl = float(layer);
        float gridScale = 8.0 + fl * 10.0;
        vec2 scaledUV = uv * gridScale;

        for (int dy = -1; dy <= 1; dy++) {
            for (int dx = -1; dx <= 1; dx++) {
                vec2 cell = floor(scaledUV) + vec2(float(dx), float(dy));
                float h = hash(cell + fl * 137.0 + trackSeed);
                if (h > 1.0 - density * 0.5 * wDots) {
                    vec2 offset = hash2(cell + fl * 200.0 + trackSeed) * 0.5 - 0.25;
                    float h2 = hash(cell + fl * 337.0 + trackSeed);
                    float r = (0.02 + h2 * 0.10) * sz;
                    r *= 0.85 + 0.15 * sin(t * 2.0 + h * 6.28);

                    float wobble = 0.15 + complexity * 0.35;
                    float d = sdWobblyCircle(scaledUV, cell + 0.5 + offset, r, wobble, h * 100.0 + trackSeed);
                    result = max(result, 1.0 - smoothstep(0.0, soft * gridScale, d));

                    if (h2 > 0.5 && complexity > 0.3) {
                        for (int sp = 0; sp < 3; sp++) {
                            float fsp = float(sp);
                            float spAng = hash(cell + fsp * 41.0 + trackSeed + 500.0) * 6.28;
                            float spDist = r * (1.3 + hash(cell + fsp * 43.0 + trackSeed) * 1.2);
                            vec2 spPos = cell + 0.5 + offset + vec2(cos(spAng), sin(spAng)) * spDist;
                            float spR = r * (0.1 + hash(cell + fsp * 47.0 + trackSeed) * 0.2);
                            float spD = length(scaledUV - spPos) - spR;
                            result = max(result, 1.0 - smoothstep(0.0, soft * gridScale, spD));
                        }
                    }
                }
            }
        }
    }

    // === 2. CONNECTED NODES (constellation) ===
    float nodeGrid = 1.5;
    vec2 nodeScaled = uv * nodeGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(nodeScaled) + vec2(float(cx), float(cy));
            float ch = hash(cell + trackSeed + 600.0);
            if (ch > 1.0 - density * 0.5 * wNodes) {
                int numNodes = 3 + int(complexity * 4.0);
                vec2 prevNode = vec2(0.0);

                for (int i = 0; i < 7; i++) {
                    if (i >= numNodes) break;
                    float fi = float(i);
                    vec2 npos = (cell + hash2(cell + fi * 31.0 + trackSeed + 620.0)) / nodeGrid;
                    npos.y = trackY + (npos.y - trackY) * ySpread * 3.0;
                    float r = (0.001 + hash(cell + fi * 37.0 + trackSeed + 650.0) * 0.012) * sz;

                    float d = sdWobblyCircle(uv, npos, r, 0.15, ch * 30.0 + fi);
                    result = max(result, 1.0 - smoothstep(0.0, soft, d));

                    // Thin connecting line to previous node
                    if (i > 0) {
                        float lineW = 0.0004 + sz * 0.0006;
                        float ld = sdSegment(uv, prevNode, npos) - lineW;
                        result = max(result, 1.0 - smoothstep(0.0, soft, ld));
                    }
                    prevNode = npos;
                }
            }
        }
    }

    // === 3. METABALL BLOBS + TENDRILS ===
    float blobGrid = 2.5;
    vec2 blobScaled = uv * blobGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(blobScaled) + vec2(float(cx), float(cy));
            float ch = hash(cell + trackSeed + 1000.0);
            if (ch > 1.0 - density * 0.25 * wBlobs) {
                vec2 center = (cell + hash2(cell + trackSeed + 1100.0)) / blobGrid;
                center.y = trackY + (center.y - trackY) * ySpread * 2.0;

                float field = 0.0;
                int numBalls = 2 + int(complexity * 4.0);
                for (int i = 0; i < 9; i++) {
                    if (i >= numBalls) break;
                    float fi = float(i);
                    float bs = fi * 7.3 + ch * 50.0 + trackSeed;
                    vec2 pos = center + vec2(
                        noise(vec2(bs, t * 0.35)) * 0.06 - 0.03,
                        noise(vec2(bs + 100.0, t * 0.3)) * 0.03 - 0.015
                    ) * sz;

                    float r = (0.003 + noise(vec2(fi * 3.1 + trackSeed, t * 0.2 + ch * 30.0)) * 0.01) * sz;

                    float elongAng = hash(vec2(fi, ch) + trackSeed + 1150.0) * 3.14;
                    float elong = 1.0 + hash(vec2(fi + 0.5, ch) + trackSeed + 1160.0) * 2.0;
                    vec2 dd = uv - pos;
                    vec2 eDir = vec2(cos(elongAng), sin(elongAng));
                    vec2 ePerp = vec2(-eDir.y, eDir.x);
                    float distSq = dot(dd, eDir) * dot(dd, eDir) / (elong * elong) + dot(dd, ePerp) * dot(dd, ePerp);
                    field += r * r / (distSq + 0.00003);
                }
                result = max(result, smoothstep(0.9, 1.0 + soft * 10.0, field));

                // Curling tendrils
                int numTen = 1 + int(complexity * 3.0);
                for (int ten = 0; ten < 4; ten++) {
                    if (ten >= numTen) break;
                    float ft = float(ten);
                    float th = hash(cell + ft * 17.0 + trackSeed + 1200.0);
                    vec2 start = center + vec2(
                        (hash(cell + ft * 31.0 + trackSeed + 1300.0) - 0.5) * 0.06,
                        (hash(cell + ft * 33.0 + trackSeed + 1310.0) - 0.5) * 0.03
                    ) * sz;
                    float curlAng = hash(cell + ft * 37.0 + trackSeed + 1400.0) * 6.28;
                    float tLen = (0.03 + hash(cell + ft * 41.0 + trackSeed + 1500.0) * 0.08) * sz;

                    vec2 prev = start;
                    for (int seg = 0; seg < 8; seg++) {
                        float fs = float(seg) / 8.0;
                        curlAng += (noise(vec2(fs * 5.0 + th * 20.0, t * 0.3)) - 0.5) * 1.5;
                        vec2 next = prev + vec2(cos(curlAng), sin(curlAng)) * tLen * 0.14;
                        float w = (0.002 + sz * 0.003) * (1.0 - fs * 0.85);
                        w *= 0.7 + 0.3 * noise(vec2(fs * 10.0 + th * 30.0, t * 0.2));
                        float dd = sdSegment(uv, prev, next) - w;
                        result = max(result, 1.0 - smoothstep(0.0, soft, dd));
                        prev = next;
                    }
                    float endR = (0.002 + hash(cell + ft * 43.0 + trackSeed) * 0.004) * sz;
                    float endD = sdWobblyCircle(uv, prev, endR, 0.3, th * 50.0);
                    result = max(result, 1.0 - smoothstep(0.0, soft, endD));
                }
            }
        }
    }

    // === 4. CALLIGRAPHIC STROKES ===
    float curveGrid = 1.0;
    vec2 curveScaled = uv * curveGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(curveScaled) + vec2(float(cx), float(cy));
            float ch = hash(cell + trackSeed + 2000.0);

            if (ch > 1.0 - density * 0.7 * wStrokes) {
                vec2 origin = (cell + hash2(cell + trackSeed + 2100.0)) / curveGrid;
                origin.y = trackY + (origin.y - trackY) * ySpread * 2.0;

                float shapeType = hash(cell + trackSeed + 2050.0);
                // Much longer strokes - range from medium to very long
                float curveLen = (0.1 + hash(cell + trackSeed + 2400.0) * 0.5) * sz;
                float startAng = (hash(cell + trackSeed + 2600.0) - 0.5) * 0.6;

                vec2 prev = origin;
                float ang = startAng;
                int numSegs = max(8 + int(complexity * 8.0), 1);

                for (int seg = 0; seg < 16; seg++) {
                    if (seg >= numSegs) break;
                    float fs = float(seg) / float(numSegs);

                    float curvature;
                    if (shapeType < 0.2) {
                        // Long thin sweep - nearly straight, slight wave
                        curvature = sin(fs * 3.14 + ch * 3.0) * 0.4;
                    } else if (shapeType < 0.35) {
                        // Spiral tightening
                        curvature = 0.2 + fs * 2.0;
                    } else if (shapeType < 0.5) {
                        // Hook - straight then sharp curve at end
                        curvature = smoothstep(0.5, 0.85, fs) * 4.0;
                    } else if (shapeType < 0.65) {
                        // S-curve
                        curvature = sin(fs * 6.28 + ch * 5.0) * 1.5;
                    } else if (shapeType < 0.8) {
                        // Organic swoosh
                        curvature = (noise(vec2(fs * 6.0 + ch * 20.0, t * 0.3 + trackSeed)) - 0.5) * 2.5;
                    } else {
                        // Wide gentle arc
                        curvature = (hash(cell + trackSeed + 2650.0) - 0.5) * 1.2;
                    }

                    ang += curvature * curveLen * 0.15;
                    float stepLen = curveLen / float(numSegs);
                    stepLen *= 0.8 + 0.4 * noise(vec2(fs * 4.0 + ch * 10.0, trackSeed));
                    vec2 next = prev + vec2(cos(ang), sin(ang)) * stepLen;

                    // Thinner strokes with more pressure variation
                    float baseW = 0.0008 + sz * 0.003;
                    float pressure;
                    if (shapeType < 0.2) {
                        // Long sweep: very thin, uniform
                        pressure = 0.15 + 0.15 * sin(fs * 3.14);
                    } else if (shapeType < 0.35) {
                        // Spiral: thick start, thin end
                        pressure = 1.0 - fs * 0.8;
                    } else if (shapeType < 0.5) {
                        // Hook: medium, bulge at bend
                        pressure = 0.3 + 0.7 * sin(fs * 3.14);
                    } else {
                        // Others: organic pressure
                        pressure = 0.15 + 0.85 * noise(vec2(fs * 8.0 + ch * 15.0, t * 0.15 + trackSeed));
                    }
                    pressure *= smoothstep(0.0, 0.06, fs) * smoothstep(1.0, 0.9, fs);
                    float w = baseW * pressure;

                    float dd = sdSegment(uv, prev, next) - w;
                    result = max(result, 1.0 - smoothstep(0.0, soft, dd));
                    prev = next;
                }

                // Terminal flourish - blob at end of stroke
                if (hash(cell + trackSeed + 2700.0) > 0.4) {
                    float endR = (0.001 + sz * 0.004) * (0.3 + hash(cell + trackSeed + 2800.0) * 0.7);
                    float endD = sdWobblyCircle(uv, prev, endR, 0.3, ch * 70.0);
                    result = max(result, 1.0 - smoothstep(0.0, soft, endD));
                }
            }
        }
    }

    // === 5. HATCHING CLUSTERS ===
    float hatchGrid = 2.5;
    vec2 hatchScaled = uv * hatchGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(hatchScaled) + vec2(float(cx), float(cy));
            float hh = hash(cell + trackSeed + 3000.0);
            if (hh > 1.0 - density * 0.35 * wHatch && complexity > 0.2) {
                vec2 center = (cell + hash2(cell + trackSeed + 3100.0)) / hatchGrid;
                center.y = trackY + (center.y - trackY) * ySpread * 2.0;

                float hatchAng = (hash(cell + trackSeed + 3200.0) - 0.5) * 1.5;
                float hatchW = (0.02 + hash(cell + trackSeed + 3300.0) * 0.05) * sz;
                float hatchH = (0.005 + hash(cell + trackSeed + 3400.0) * 0.015) * sz;
                int numLines = 4 + int(complexity * 10.0);
                float spacing = hatchW / float(numLines);

                vec2 dir = vec2(cos(hatchAng), sin(hatchAng));
                vec2 perp = vec2(-dir.y, dir.x);

                for (int li = 0; li < 14; li++) {
                    if (li >= numLines) break;
                    float fli = float(li) - float(numLines) * 0.5;
                    vec2 lineCenter = center + dir * fli * spacing;
                    float lineLen = hatchH * (0.5 + noise(vec2(float(li) * 3.0 + hh * 20.0, trackSeed + 3500.0)));
                    vec2 a = lineCenter - perp * lineLen;
                    vec2 b = lineCenter + perp * lineLen;
                    float w = 0.0004 + sz * 0.0005;
                    float d = sdSegment(uv, a, b) - w;
                    result = max(result, 1.0 - smoothstep(0.0, soft, d));
                }
            }
        }
    }

    // === 6. DRIPS / PENDANTS ===
    float dripGrid = 3.5;
    vec2 dripScaled = uv * dripGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(dripScaled) + vec2(float(cx), float(cy));
            float dh = hash(cell + trackSeed + 4000.0);
            if (dh > 1.0 - density * 0.4 * wDrips) {
                vec2 top = (cell + hash2(cell + trackSeed + 4100.0)) / dripGrid;
                top.y = trackY;

                float dripLen = (0.015 + hash(cell + trackSeed + 4200.0) * 0.05) * sz;
                float dripW = (0.001 + hash(cell + trackSeed + 4300.0) * 0.003) * sz;
                // Mostly downward, slight random angle
                float dripAng = -1.5708 + (hash(cell + trackSeed + 4400.0) - 0.5) * 0.4;
                vec2 bottom = top + vec2(cos(dripAng), sin(dripAng)) * dripLen;

                vec2 prev = top;
                for (int seg = 1; seg <= 6; seg++) {
                    float fs = float(seg) / 6.0;
                    vec2 pt = mix(top, bottom, fs);
                    pt.x += noise(vec2(fs * 5.0 + dh * 20.0, trackSeed + 4500.0)) * 0.002 * sz;
                    // Thin at top, wider toward bottom
                    float w = dripW * (0.3 + 1.5 * fs * fs);
                    float d = sdSegment(uv, prev, pt) - w;
                    result = max(result, 1.0 - smoothstep(0.0, soft, d));
                    prev = pt;
                }
                // Teardrop bulge at bottom
                float bulgeR = dripW * 2.0;
                float bd = length(uv - bottom) - bulgeR;
                result = max(result, 1.0 - smoothstep(0.0, soft, bd));
            }
        }
    }

    // === 7. LEAF / PETAL SHAPES ===
    float leafGrid = 4.0;
    vec2 leafScaled = uv * leafGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(leafScaled) + vec2(float(cx), float(cy));
            float lh = hash(cell + trackSeed + 5000.0);
            if (lh > 1.0 - density * 0.45 * wLeaves) {
                vec2 center = (cell + hash2(cell + trackSeed + 5100.0)) / leafGrid;
                center.y = trackY + (center.y - trackY) * ySpread * 3.0;

                float leafAng = (hash(cell + trackSeed + 5200.0) - 0.5) * 2.5;
                float leafLen = (0.008 + hash(cell + trackSeed + 5300.0) * 0.025) * sz;
                float leafW = leafLen * (0.2 + hash(cell + trackSeed + 5400.0) * 0.4);

                vec2 dir = vec2(cos(leafAng), sin(leafAng));
                vec2 a = center - dir * leafLen;
                vec2 b = center + dir * leafLen;

                vec2 pa = uv - a;
                vec2 ba = b - a;
                float baDot = dot(ba, ba);
                float tParam = (baDot > 0.0001) ? clamp(dot(pa, ba) / baDot, 0.0, 1.0) : 0.5;
                float dist = length(pa - ba * tParam);
                // Sinusoidal width profile: pointed ends, wide middle
                float w = leafW * sin(tParam * 3.14159);
                float d = dist - w;
                result = max(result, 1.0 - smoothstep(0.0, soft, d));
            }
        }
    }

    // === 8. OUTLINE RECTANGLES ===
    float rectGrid = 3.0;
    vec2 rectScaled = uv * rectGrid;
    for (int cy = -1; cy <= 1; cy++) {
        for (int cx = -1; cx <= 1; cx++) {
            vec2 cell = floor(rectScaled) + vec2(float(cx), float(cy));
            float rh = hash(cell + trackSeed + 6000.0);
            if (rh > 1.0 - density * 0.25 * wRects) {
                vec2 center = (cell + hash2(cell + trackSeed + 6100.0)) / rectGrid;
                center.y = trackY + (center.y - trackY) * ySpread * 2.0;

                float rectAng = (hash(cell + trackSeed + 6200.0) - 0.5) * 0.5;
                vec2 halfSize = vec2(
                    (0.005 + hash(cell + trackSeed + 6300.0) * 0.015) * sz,
                    (0.003 + hash(cell + trackSeed + 6400.0) * 0.01) * sz
                );

                // Rotate point into box local space
                vec2 d = uv - center;
                float rc = cos(rectAng);
                float rs = sin(rectAng);
                vec2 ld = vec2(d.x * rc + d.y * rs, -d.x * rs + d.y * rc);
                float box = sdBox(ld, halfSize);
                // Outline only
                float outline = abs(box) - (0.0005 + sz * 0.0005);
                result = max(result, 1.0 - smoothstep(0.0, soft, outline));
            }
        }
    }

    return result * amt;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    uv /= scale;

    // Scroll
    vec2 scrollDir = vec2(cos(scrollAngle), sin(scrollAngle));
    uv += scrollDir * TIME * scrollSpeed * 0.2;

    float t = TIME * speed;

    // 3 tracks like a graphic score
    float y1 = 0.25 / scale;
    float y2 = 0.50 / scale;
    float y3 = 0.75 / scale;

    // Account for scroll offset on y
    float scrollY = scrollDir.y * TIME * scrollSpeed * 0.2;
    y1 += scrollY;
    y2 += scrollY;
    y3 += scrollY;

    float col = 0.0;
    col = max(col, generateTrack(uv, y1, t, seed + 0.0, track1));
    col = max(col, generateTrack(uv, y2, t, seed + 33.0, track2));
    col = max(col, generateTrack(uv, y3, t, seed + 67.0, track3));

    // Staff lines - thin organic horizontal lines at each track position
    if (lineThickness > 0.01) {
        float lineW = 0.0005 + lineThickness * 0.002;
        float lineSoft = lineW * 0.8;

        // Line 1
        float wave1 = noise(vec2(uv.x * 8.0 + seed, 43.0 + seed)) * 0.003
                    + noise(vec2(uv.x * 25.0 + seed, 71.0)) * 0.001;
        float dist1 = abs(uv.y - y1 - wave1);
        float tv1 = 0.7 + 0.3 * noise(vec2(uv.x * 12.0 + seed, seed));
        col = max(col, (1.0 - smoothstep(0.0, lineW * tv1 + lineSoft, dist1)) * lineThickness);

        // Line 2
        float wave2 = noise(vec2(uv.x * 8.0 + 17.0 + seed, 86.0 + seed)) * 0.003
                    + noise(vec2(uv.x * 25.0 + 31.0 + seed, 142.0)) * 0.001;
        float dist2 = abs(uv.y - y2 - wave2);
        float tv2 = 0.7 + 0.3 * noise(vec2(uv.x * 12.0 + 53.0, seed + 19.0));
        col = max(col, (1.0 - smoothstep(0.0, lineW * tv2 + lineSoft, dist2)) * lineThickness);

        // Line 3
        float wave3 = noise(vec2(uv.x * 8.0 + 34.0 + seed, 129.0 + seed)) * 0.003
                    + noise(vec2(uv.x * 25.0 + 62.0 + seed, 213.0)) * 0.001;
        float dist3 = abs(uv.y - y3 - wave3);
        float tv3 = 0.7 + 0.3 * noise(vec2(uv.x * 12.0 + 106.0, seed + 38.0));
        col = max(col, (1.0 - smoothstep(0.0, lineW * tv3 + lineSoft, dist3)) * lineThickness);
    }

    col = clamp(col, 0.0, 1.0);
    gl_FragColor = vec4(vec3(col), 1.0);
}
