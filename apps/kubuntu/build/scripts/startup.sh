#!/bin/bash -e

source /opt/gow/bash-lib/utils.sh

# Run additional startup scripts
for file in /opt/gow/startup.d/* ; do
    if [ -f "$file" ] ; then
        gow_log "[start] Sourcing $file"
        source $file
    fi
done

gow_log "[start] Starting KDE Plasma"

# Run Plasma inside Sway so its window fills the stream (see launch-comp.sh). RUN_SWAY= in the env starts it bare.
export RUN_SWAY="${RUN_SWAY-1}"
source /opt/gow/launch-comp.sh
launcher
