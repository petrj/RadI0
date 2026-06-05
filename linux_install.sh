#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_DIR="$SCRIPT_DIR/RadI0/bin/Release/net10.0/linux-x64"
TARGET_DIR="/opt/RadI0"

if [ ! -d "$RELEASE_DIR" ]; then
  printf "[ERROR] %s\n" "Release directory not found: $RELEASE_DIR" >&2
  printf "[ERROR] %s\n" "Build the Linux release first and rerun this script." >&2
  exit 1
fi

if ! find "$RELEASE_DIR" -mindepth 1 -print -quit >/dev/null 2>&1; then
  printf "[ERROR] %s\n" "No files found in release directory: $RELEASE_DIR" >&2
  exit 1
fi

printf "[INFO] %s\n" "Found release directory: $RELEASE_DIR"

if [ ! -d "$TARGET_DIR" ]; then
  printf "[INFO] %s\n" "$TARGET_DIR does not exist. Creating it with sudo."
  sudo mkdir -p "$TARGET_DIR"
  sudo chown -R "$(whoami)":"$(whoami)" "$TARGET_DIR"
else
  printf "[INFO] %s\n" "$TARGET_DIR already exists."
fi

if [ ! -w "$TARGET_DIR" ]; then
  printf "[INFO] %s\n" "$TARGET_DIR is not writable by $(whoami). Adjusting ownership with sudo."
  sudo chown -R "$(whoami)":"$(whoami)" "$TARGET_DIR"
fi

if [ -n "$(find "$TARGET_DIR" -mindepth 1 -print -quit)" ]; then
  printf "[INFO] %s\n" "Removing existing files from $TARGET_DIR"
  find "$TARGET_DIR" -mindepth 1 -maxdepth 1 -exec rm -rf -- {} +
fi

printf "[INFO] %s\n" "Copying release files to $TARGET_DIR"
cp -av "$RELEASE_DIR"/. "$TARGET_DIR"

MAIN_EXECUTABLE="$TARGET_DIR/RadI0"
if [ -e "$MAIN_EXECUTABLE" ]; then
  printf "[INFO] %s\n" "Making $MAIN_EXECUTABLE executable"
  chmod +x "$MAIN_EXECUTABLE"
else
  printf "[INFO] %s\n" "Main executable $MAIN_EXECUTABLE not found, skipping chmod."
fi

DESKTOP_FILE="$TARGET_DIR/RadI0.desktop"
if [ -e "$DESKTOP_FILE" ]; then
  printf "[INFO] %s\n" "Copying $DESKTOP_FILE to ~/Desktop"
  cp "$DESKTOP_FILE" "$HOME/Desktop/"
else
  printf "[INFO] %s\n" "Desktop file $DESKTOP_FILE not found, skipping desktop copy."
fi

printf "[INFO] %s\n" "Installation complete. Files copied to $TARGET_DIR"
