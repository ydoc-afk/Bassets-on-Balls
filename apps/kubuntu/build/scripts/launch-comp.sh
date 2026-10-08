#!/bin/bash
set -e

source /opt/gow/bash-lib/utils.sh

function launcher() {
  export XDG_DATA_DIRS=/var/lib/flatpak/exports/share:/home/retro/.local/share/flatpak/exports/share:/usr/local/share/:/usr/share/

  if [ ! -d "$HOME/.config/kwinrc" ] && [ ! -f "$HOME/.config/kwinrc" ]; then
    # First run: add flathub repo
    flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo

    # Create common folders
    mkdir -p ~/Desktop ~/Documents ~/Downloads ~/Music ~/Pictures ~/Public ~/Templates ~/Videos
    chmod 755 ~/Desktop ~/Documents ~/Downloads ~/Music ~/Pictures ~/Public ~/Templates ~/Videos
  fi

  #
  # Launch DBUS
  sudo /opt/gow/startdbus

  export DESKTOP_SESSION=plasma
  export XDG_CURRENT_DESKTOP=KDE
  export XDG_SESSION_TYPE="wayland"
  export _JAVA_AWT_WM_NONREPARENTING=1
  export QT_QPA_PLATFORM="wayland"
  export QT_AUTO_SCREEN_SCALE_FACTOR=1
  export QT_ENABLE_HIGHDPI_SCALING=1
  export KDE_FULL_SESSION=1
  # Keep WAYLAND_DISPLAY: Plasma (KWin) runs as a Wayland client of the
  # virtual display, the same way Sway does in the base-app image.

  #
  # Start the Plasma Wayland session
  exec dbus-run-session -- startplasma-wayland
}
