#!/bin/bash
# KWin's Xwayland refuses to start without this directory
mkdir -p /tmp/.X11-unix
chmod 1777 /tmp/.X11-unix
service dbus start
