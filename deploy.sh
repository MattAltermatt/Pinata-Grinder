#!/usr/bin/env bash
#
# Deploy a local Unity WebGL build to GitHub Pages (gh-pages branch).
#
# Usage:
#   1. In Unity: File > Build Settings > WebGL > Build  (output to "Build/WebGL")
#   2. Run:  ./deploy.sh
#
# The script pushes ONLY the build output to the gh-pages branch.
# Your main branch and working tree are not affected.

set -euo pipefail

BUILD_DIR="Build/WebGL"

# Verify build exists
if [ ! -f "$BUILD_DIR/index.html" ]; then
    echo "ERROR: No WebGL build found at $BUILD_DIR/index.html"
    echo ""
    echo "Build your project first:"
    echo "  Unity > File > Build Settings > WebGL > Build"
    echo "  Set the output folder to: Build/WebGL"
    exit 1
fi

echo "Deploying WebGL build from $BUILD_DIR to gh-pages..."

# Create a temporary directory for the deployment
DEPLOY_DIR=$(mktemp -d)
trap 'rm -rf "$DEPLOY_DIR"' EXIT

# Copy build output
cp -R "$BUILD_DIR/." "$DEPLOY_DIR/"

# Initialize a fresh git repo in the temp dir and push to gh-pages
cd "$DEPLOY_DIR"
git init -q
git checkout -q -b gh-pages
git add .
git commit -q -m "Deploy WebGL build $(date '+%Y-%m-%d %H:%M:%S')"
git remote add origin "$(cd "$OLDPWD" && git remote get-url origin)"
git push --force origin gh-pages

echo ""
echo "Deployed! Your game will be live at:"
echo "  https://mattaltermatt.github.io/Pinata-Grinder/"
echo ""
echo "Note: GitHub Pages may take 1-2 minutes to update."
