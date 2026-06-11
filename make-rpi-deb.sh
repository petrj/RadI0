#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_FILE="$ROOT_DIR/RadI0/RadI0.csproj"
VERSION_FILE="$ROOT_DIR/version.txt"
DESKTOP_SRC="$ROOT_DIR/RadI0.desktop"
ICON_SRC="$ROOT_DIR/Graphics/icon.png"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR] dotnet is not installed or not in PATH. Install .NET 10 SDK/runtime first."
  exit 1
fi

if ! command -v dpkg-deb >/dev/null 2>&1; then
  echo "[ERROR] dpkg-deb is not installed. Install dpkg-dev."
  exit 1
fi

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "[ERROR] Project file not found: $PROJECT_FILE"
  exit 1
fi

if [[ ! -f "$VERSION_FILE" ]]; then
  echo "[ERROR] Version file not found: $VERSION_FILE"
  exit 1
fi

VERSION="$(<"$VERSION_FILE")"
VERSION="${VERSION##*( )}"
VERSION="${VERSION%%*( )}"

RUNTIME="${1:-linux-arm64}"
SELF_CONTAINED=false
PACKAGE_NAME="radi0"
INSTALL_PREFIX="/opt/RadI0"
BIN_NAME="RadI0"
OUTPUT_DIR="$ROOT_DIR/build-deb"
PUBLISH_DIR="$OUTPUT_DIR/publish/$RUNTIME"
PKG_ROOT="$OUTPUT_DIR/package"
DEBIAN_DIR="$PKG_ROOT/DEBIAN"

case "$RUNTIME" in
  linux-arm)
    DEB_ARCH="armhf"
    SELF_CONTAINED=true
    ;;
  linux-arm64)
    DEB_ARCH="arm64"
    SELF_CONTAINED=true
    ;;
  linux-x64)
    DEB_ARCH="amd64"
    ;;
  *)
    echo "[ERROR] Unsupported runtime: $RUNTIME"
    echo "Supported runtimes: linux-arm linux-arm64 linux-x64"
    exit 1
    ;;
esac

if [[ "${2:-}" == "--self-contained" ]]; then
  SELF_CONTAINED=true
elif [[ "${2:-}" == "--not-self-contained" ]]; then
  SELF_CONTAINED=false
fi

rm -rf "$OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR" "$DEBIAN_DIR" \
  "$PKG_ROOT$INSTALL_PREFIX" \
  "$PKG_ROOT/usr/bin" \
  "$PKG_ROOT/usr/share/applications" \
  "$PKG_ROOT/usr/share/pixmaps"

publish_args=(publish "$PROJECT_FILE" -c Release -r "$RUNTIME" -o "$PUBLISH_DIR" -p:PublishTrimmed=false -p:DebugType=None)
if [[ "$SELF_CONTAINED" == true ]]; then
  publish_args+=(--self-contained true)
else
  publish_args+=(--self-contained false)
fi

# Allow dotnet to restore automatically when needed.

echo "[INFO] Publishing RadI0 for runtime $RUNTIME"
dotnet "${publish_args[@]}"

if [[ ! -f "$PUBLISH_DIR/$BIN_NAME" ]]; then
  echo "[ERROR] Published executable not found in $PUBLISH_DIR"
  exit 1
fi

cp -a "$PUBLISH_DIR/"* "$PKG_ROOT$INSTALL_PREFIX/"
chmod +x "$PKG_ROOT$INSTALL_PREFIX/$BIN_NAME"
ln -s "$INSTALL_PREFIX/$BIN_NAME" "$PKG_ROOT/usr/bin/$BIN_NAME"

if [[ -f "$ICON_SRC" ]]; then
  cp "$ICON_SRC" "$PKG_ROOT/usr/share/pixmaps/${PACKAGE_NAME}.png"
fi

cat > "$PKG_ROOT/usr/share/applications/${PACKAGE_NAME}.desktop" <<'DESKTOP'
[Desktop Entry]
Version=1.0
Type=Application
Name=RadI0
Exec=/opt/RadI0/RadI0
Path=/opt/RadI0
Terminal=true
Icon=/usr/share/pixmaps/radi0.png
Categories=Audio;Application;
Comment=RadI0 SDR receiver for DAB+ and FM radio
DESKTOP

CONTROL_FILE="$DEBIAN_DIR/control"
INSTALLED_SIZE=$(du -s "$PKG_ROOT" | cut -f1)

if [[ "$SELF_CONTAINED" == true ]]; then
  RUNTIME_DEPENDS=""
else
  RUNTIME_DEPENDS="dotnet-runtime-10.0"
fi

DEPENDS_LINE="$RUNTIME_DEPENDS"
if [[ -n "$DEPENDS_LINE" ]]; then
  DEPENDS_LINE=", $DEPENDS_LINE"
fi
DEPENDS_LINE="rtl-sdr, libfaad2, libasound2, vlc, libvlc-bin$DEPENDS_LINE"

cat > "$CONTROL_FILE" <<EOF
Package: $PACKAGE_NAME
Version: $VERSION
Section: sound
Priority: optional
Architecture: $DEB_ARCH
Installed-Size: $INSTALLED_SIZE
Maintainer: RadI0 Packaging <noreply@example.com>
Depends: $DEPENDS_LINE
Description: RadI0 SDR receiver for DAB+ and FM broadcasts
 RadI0 is a .NET 10 terminal-based software-defined radio receiver for Raspberry Pi.
EOF

BIN_OUTPUT_DIR="$ROOT_DIR/RadI0/bin"
mkdir -p "$BIN_OUTPUT_DIR"
PACKAGE_FILE="$BIN_OUTPUT_DIR/${PACKAGE_NAME}_${VERSION}_${DEB_ARCH}.deb"

echo "[INFO] Building Debian package $PACKAGE_FILE"
dpkg-deb --build "$PKG_ROOT" "$PACKAGE_FILE"

rm -rf "$OUTPUT_DIR"

echo "[INFO] Debian package created: $PACKAGE_FILE"
