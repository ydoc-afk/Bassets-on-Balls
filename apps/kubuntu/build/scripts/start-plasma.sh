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

# KWin runs as a Wayland client of the compositor it is started in (it picks that backend because WAYLAND_DISPLAY is
# set) and starts its own Xwayland for X11 apps.
exec dbus-run-session -- startplasma-wayland
