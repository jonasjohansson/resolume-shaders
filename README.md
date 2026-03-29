# Resolume ISF Shaders

Custom ISF (Interactive Shader Format) shaders compiled as FFGL plugins for Resolume Arena.

Each effect lives in its own folder with the `.fs` source and compiled `.bundle`.

## Effects

| Shader | Description |
|--------|-------------|
| AquarelaMask | Watercolor paint effect with Kuwahara smoothing and gradient mask |
| BlurMask | Gaussian blur with gradient mask control |
| EdgeGrow | Organic lichen-like growth extending from edges of source shapes |
| EnergyPulse | Expanding pulse that illuminates shapes as it sweeps through |
| GhostTrail | Ethereal ghost copies drifting from source shapes |
| GradientAlpha | Gradient-based alpha control |
| PulseRings | Concentric rings rippling outward from shape edges |
| ShapeGen | Graphic score generator with 3 tracks of organic shapes |
| SlitScreen | Repeats boundary pixels outward from a gradient mask edge |
| SmartVignette | Vignette with round/square modes, movable center, optional image mask |
| SmokeDissipation | Content wisps away like rising smoke with curl noise turbulence |
| WarpFBM | Domain-warped FBM that organically warps source content with animated noise |

Most effects share a common gradient mask system: `maskPos`, `maskWidth`, `fadeWidth`, `angle`, `radial`, `invert`.

## Compiling ISF to FFGL

Shaders must be compiled into `.bundle` FFGL plugins for Resolume to load them. This uses [ffgl-rs](https://github.com/edeetee/ffgl-rs).

### Prerequisites

- **Rust**: `curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh`
- **Xcode CLI tools**: `xcode-select --install`
- **ffgl-rs repo**: cloned at `~/Documents/GitHub/ffgl-rs`

### Compile a single shader

```bash
cd ~/Documents/GitHub/ffgl-rs
bash ffgl-isf/deploy_isf.sh /path/to/ShaderName/ShaderName.fs
```

This compiles the ISF shader and deploys the `.bundle` to:
- `~/Documents/Resolume Arena/Extra Effects/`
- `~/Library/Graphics/FreeFrame Plug-Ins/`

### Compile all shaders

```bash
cd ~/Documents/GitHub/ffgl-rs
for f in ~/Documents/GitHub/resolume-ffgl/*/*.fs; do
  bash ffgl-isf/deploy_isf.sh "$f"
done
```

### After compiling

Restart Resolume Arena to pick up new/updated effects.
