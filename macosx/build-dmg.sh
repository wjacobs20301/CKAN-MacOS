#!/bin/sh
# Build a styled DMG with a background image, a drag-to-Applications
# target, and fixed icon positions.
#
# Usage: build-dmg.sh <volume-name> <source-dir> <output-dmg> <background-image>
#
# The source dir is expected to already contain CKAN.app (the bundle). This
# script adds a /Applications symlink and a hidden .background folder, sets
# Finder view options via AppleScript, then converts to a compressed
# read-only DMG.

set -e

VOLNAME="$1"
SRCDIR="$2"
OUTDMG="$3"
BGIMG="$4"

APPNAME="${VOLNAME}.app"
TMPDMG="$(dirname "$OUTDMG")/.${VOLNAME}-rw.dmg"
MOUNTROOT="/Volumes/${VOLNAME}"

if [ ! -d "$SRCDIR/$APPNAME" ]; then
  echo "ERROR: $SRCDIR/$APPNAME not found"
  exit 1
fi
if [ ! -f "$BGIMG" ]; then
  echo "ERROR: background image $BGIMG not found"
  exit 1
fi

echo "Preparing DMG staging area..."
# Add the Applications symlink and hidden background folder to the staging dir.
rm -f  "$SRCDIR/Applications"
ln -s  /Applications "$SRCDIR/Applications"
rm -rf "$SRCDIR/.background"
mkdir  "$SRCDIR/.background"
cp     "$BGIMG" "$SRCDIR/.background/background.jpg"

# Figure out size needed for the RW DMG (staging + 20 MB slack).
SIZE_KB=$(du -sk "$SRCDIR" | awk '{print $1}')
SIZE_MB=$(( SIZE_KB / 1024 + 40 ))

echo "Creating temporary R/W DMG (${SIZE_MB}M)..."
rm -f "$TMPDMG"
hdiutil create -volname "$VOLNAME" -srcfolder "$SRCDIR" \
  -fs HFS+ -format UDRW -size "${SIZE_MB}m" "$TMPDMG" >/dev/null

echo "Mounting..."
# If something is already mounted at that path, detach it.
if [ -d "$MOUNTROOT" ]; then
  hdiutil detach "$MOUNTROOT" -quiet || true
fi
DEV=$(hdiutil attach -readwrite -noverify -noautoopen "$TMPDMG" \
  | egrep '^/dev/' | head -1 | awk '{print $1}')

# Give Finder a moment to notice the new volume.
sleep 2

echo "Applying Finder view options..."
osascript <<OSA
tell application "Finder"
    tell disk "${VOLNAME}"
        open
        set current view of container window to icon view
        set toolbar visible of container window to false
        set statusbar visible of container window to false
        set the bounds of container window to {200, 120, 1100, 648}
        set theViewOptions to the icon view options of container window
        set arrangement of theViewOptions to not arranged
        set icon size of theViewOptions to 128
        set background picture of theViewOptions to file ".background:background.jpg"
        set position of item "${APPNAME}"      of container window to {230, 340}
        set position of item "Applications"    of container window to {670, 340}
        close
        open
        update without registering applications
        delay 2
    end tell
end tell
OSA

# Make sure the changes are flushed before we unmount.
sync

echo "Detaching..."
hdiutil detach "$DEV" -quiet || hdiutil detach "$DEV" -force

echo "Converting to compressed read-only DMG..."
rm -f "$OUTDMG"
hdiutil convert "$TMPDMG" -format UDZO -imagekey zlib-level=9 -o "$OUTDMG" >/dev/null
rm -f "$TMPDMG"

echo "DMG ready: $OUTDMG"
