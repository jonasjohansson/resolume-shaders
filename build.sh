#!/bin/bash
set -e

# Build all ISF shaders into FFGL bundles
# Requires ffgl-rs at ~/Documents/GitHub/ffgl-rs

FFGL_RS="$HOME/Documents/GitHub/ffgl-rs"
REPO_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ ! -d "$FFGL_RS" ]; then
    echo "Error: ffgl-rs not found at $FFGL_RS"
    exit 1
fi

# Build each shader and copy bundle into its folder
for shader in "$REPO_DIR"/*//*.fs; do
    dir="$(dirname "$shader")"
    name="$(basename "$shader" .fs)"

    echo "=== Building $name ==="
    cd "$FFGL_RS"
    bash ffgl-isf/deploy_isf.sh "$shader" 2>&1

    # Copy bundle from deploy location into the repo folder
    bundle_name="$(echo "$name" | cut -c1-16)"
    for deploy_dir in "$HOME/Documents/Resolume Arena/Extra Effects" "$HOME/Library/Graphics/FreeFrame Plug-Ins"; do
        if [ -d "$deploy_dir/$bundle_name.bundle" ]; then
            rm -rf "$dir/$bundle_name.bundle"
            cp -R "$deploy_dir/$bundle_name.bundle" "$dir/"
            rm -rf "$deploy_dir/$bundle_name.bundle"
        fi
    done

    echo ""
done

echo "All shaders built. Bundles are in each effect folder."
echo "Restart Resolume to pick up changes."
