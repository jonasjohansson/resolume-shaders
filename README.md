# Resolume ISF Shaders

Custom ISF (Interactive Shader Format) shaders compiled as FFGL plugins for Resolume Arena.

Each effect lives in its own folder with the `.fs` source and compiled `.bundle`.

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
for f in ~/Documents/GitHub/resolume-shaders/*//*.fs; do
  bash ffgl-isf/deploy_isf.sh "$f"
done
```

### After compiling

Restart Resolume Arena to pick up new/updated effects.

## Effects

| Shader | Description |
|--------|-------------|
| AquarelaMask | Kuwahara-based watercolor with content-aware bleeding and gradient mask |
| BlurMask | Gaussian blur with gradient mask control |
| SmokeDissipation | Animated smoke wisps with curl noise turbulence and gradient mask |
| PixelStretch | Pixel stretch that repeats boundary pixels outward from mask edge |
| GradientAlpha | Gradient-based alpha control |
| WarpFBM | Domain-warped FBM noise with fire colormap overlay |

All effects share a common gradient mask system: `maskPos`, `maskWidth`, `fadeWidth`, `angle`, `radial`, `invert`.
