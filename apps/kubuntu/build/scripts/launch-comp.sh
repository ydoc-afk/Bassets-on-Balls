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
  # Launch DBUS (also creates /tmp/.X11-unix, which KWin's Xwayland needs)
  sudo /opt/gow/startdbus

  # Heeler's compositor has no window manager, so KWin's window would keep whatever size it asks for and never fill
  # the stream (a black screen). Run the session inside Sway, which makes it fullscreen. Override with RUN_SWAY= in
  # the app's env to start Plasma bare.
  if [ -n "$RUN_SWAY" ]; then
    gow_log "[Sway] - Starting Plasma"

    export SWAYSOCK=${XDG_RUNTIME_DIR}/sway.socket
    export SWAY_STOP_ON_APP_EXIT=${SWAY_STOP_ON_APP_EXIT:-"yes"}
    export GAMESCOPE_WIDTH=${GAMESCOPE_WIDTH:-1920}
    export GAMESCOPE_HEIGHT=${GAMESCOPE_HEIGHT:-1080}

    mkdir -p $HOME/.config/sway/
    cp /cfg/sway/config $HOME/.config/sway/config
    {
      echo "output * resolution ${GAMESCOPE_WIDTH}x${GAMESCOPE_HEIGHT} position 0,0"
      echo "for_window [app_id=\".*\"] fullscreen enable"
      echo "default_border none"
      echo -n "workspace main; exec /opt/gow/start-plasma.sh"
      echo
    } >> $HOME/.config/sway/config

    dbus-run-session -- sway --unsupported-gpu
  else
    exec /opt/gow/start-plasma.sh
  fi
}
