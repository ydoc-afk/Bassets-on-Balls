#!/bin/bash
# The Plasma session itself. Run inside Sway (default), or directly when RUN_SWAY= is set.
export DESKTOP_SESSION=plasma
export XDG_CURRENT_DESKTOP=KDE
export XDG_SESSION_DESKTOP=KDE
export XDG_SESSION_TYPE="wayland"
export _JAVA_AWT_WM_NONREPARENTING=1
export QT_QPA_PLATFORM="wayland"
export QT_AUTO_SCREEN_SCALE_FACTOR=1
export QT_ENABLE_HIGHDPI_SCALING=1
export KDE_FULL_SESSION=1

# There is no password (the retro user has none), so a lock screen can never be unlocked. Switch every way of
# locking off, and the idle sleep that would lead to one. Written on every start so a stray change cannot lock you out.
kwriteconfig6 --file kscreenlockerrc --group Daemon --key Autolock false
kwriteconfig6 --file kscreenlockerrc --group Daemon --key LockOnResume false
kwriteconfig6 --file kscreenlockerrc --group Daemon --key Timeout 0
for profile in AC Battery LowBattery; do
    kwriteconfig6 --file powerdevilrc --group "$profile" --group SuspendAndShutdown --key AutoSuspendAction 0
    kwriteconfig6 --file powerdevilrc --group "$profile" --group Display --key TurnOffDisplayIdleTimeoutSec -- -1
    kwriteconfig6 --file powerdevilrc --group "$profile" --group Display --key DimDisplayWhenIdle false
done

# Kiosk mode (KIOSK=1 in the app's env): take the ways out of the desktop away. This uses KDE's own Kiosk
# restrictions, so it limits the session, not the container; anything inside it still runs as the retro user.
if [ -n "$KIOSK" ]; then
    for action in logout lock_screen shell_access run_command action/switch_user; do
        kwriteconfig6 --file kdeglobals --group "KDE Action Restrictions" --key "$action" false
    done
else
    for action in logout lock_screen shell_access run_command action/switch_user; do
        kwriteconfig6 --file kdeglobals --group "KDE Action Restrictions" --key "$action" --delete
    done
fi

# KWin runs as a Wayland client of the compositor it is started in (it picks that backend because WAYLAND_DISPLAY is
# set) and starts its own Xwayland for X11 apps.
dbus-run-session -- startplasma-wayland

# Logging out ends the session; take Sway, and so the container, down with it. (A "; killall sway" on the Sway config
# line doesn't work: Sway reads the semicolon as a command separator.)
if [ "${SWAY_STOP_ON_APP_EXIT:-yes}" = "yes" ] && [ -n "$SWAYSOCK" ]; then swaymsg exit; fi
