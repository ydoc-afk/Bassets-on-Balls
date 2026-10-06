#!/bin/bash -e

source /opt/gow/bash-lib/utils.sh

# Shared Switch data is mounted read-only from the host by Heeler. The app
# config entry mounts it as:  /home/<user>/switch:/switch:ro
# Expected host layout:
#   /home/<user>/switch/
#   ├── prod.keys        # dumped from your own console (required)
#   ├── firmware/        # full firmware folder dump (required)
#   └── games/           # NSP/XCI/NSO files
#
# Per-client state (saves, NAND, profiles, settings) lives in $HOME
# (/home/retro), which Heeler mounts from the per-client state folder.
# One-time legal notice. The marker lives in the per-client state ($HOME), so it is
# shown on first launch of a new install (or after the state is wiped on a reinstall),
# not on every launch. Bump NOTICE_VERSION to show a changed notice once again.
NOTICE_VERSION=1
NOTICE_MARKER="$HOME/.local/share/eden/.notice-v${NOTICE_VERSION}"
if [ ! -f "$NOTICE_MARKER" ]; then
    gow_log "[start] ============================== NOTICE =============================="
    gow_log "[start] This image ships ONLY the Eden emulator: no keys, firmware or games."
    gow_log "[start] You must supply your own, dumped from a Switch console you own, and"
    gow_log "[start] you are responsible for complying with the law where you live."
    gow_log "[start] ====================================================================="
    mkdir -p "$(dirname "$NOTICE_MARKER")" && touch "$NOTICE_MARKER" || true
fi

if [ -f /switch/prod.keys ]; then
    mkdir -p "$HOME/.local/share/eden/keys"
    cp -f /switch/prod.keys "$HOME/.local/share/eden/keys/prod.keys"
    gow_log "[start] Installed prod.keys from /switch"
else
    gow_log "[start] WARNING: /switch/prod.keys not found. Mount your Switch data folder (prod.keys, firmware/, games/) read-only at /switch, or Eden cannot decrypt games."
fi

# Run additional startup scripts
for file in /opt/gow/startup.d/* ; do
    if [ -f "$file" ] ; then
        gow_log "[start] Sourcing $file"
        source $file
    fi
done

gow_log "[start] Starting Eden"

# Heeler's compositor has no XWayland. Without this Qt prefers its xcb backend (DISPLAY is set),
# can't connect to :0 and the app crashes on launch, so force the Wayland platform plugin that the
# AppImage bundles.
export QT_QPA_PLATFORM=wayland

# Also hand the NVIDIA Vulkan ICD to the loader directly when the container toolkit provided one
# (additive: mesa ICDs for AMD/Intel hosts keep working).
if [ -f /etc/vulkan/icd.d/nvidia_icd.json ]; then
    export VK_ADD_DRIVER_FILES=/etc/vulkan/icd.d/nvidia_icd.json
fi
source /opt/gow/launch-comp.sh
launcher /opt/eden/AppRun
