# syntax=docker/dockerfile:1.4
ARG BASE_APP_IMAGE

# hadolint ignore=DL3006
FROM ${BASE_APP_IMAGE}

# Kubuntu (KDE Plasma) desktop, built from the Ubuntu repos that the
# base-app image already uses — the same package set Kubuntu ships,
# without the installer.

ARG CORE_PACKAGES=" \
    lsb-release \
    wget \
    gnupg2 \
    dbus-x11 \
    flatpak \
    sudo \
    "

ARG DE_PACKAGES=" \
    plasma-desktop \
    plasma-pa \
    plasma-workspace \
    breeze \
    xdg-desktop-portal-kde \
    at-spi2-core \
    "

ARG ADDITIONAL_PACKAGES=" \
    dolphin \
    konsole \
    ark \
    zip unzip p7zip-full \
    "
RUN \
    # \
    # Setup Mozilla official APT repo (mozillateam PPA dropped Plucky support) \
    apt-get update && \
    install -d -m 0755 /etc/apt/keyrings && \
    wget -q https://packages.mozilla.org/apt/repo-signing-key.gpg -O /etc/apt/keyrings/packages.mozilla.org.asc && \
    echo "deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main" > /etc/apt/sources.list.d/mozilla.list && \
    printf 'Package: *\nPin: origin packages.mozilla.org\nPin-Priority: 1000\n' > /etc/apt/preferences.d/mozilla && \
    apt-get update && \
    apt-get install -y --no-install-recommends firefox && \
    # \
    # Install core packages \
    apt-get install -y $CORE_PACKAGES && \
    # \
    # Install the desktop \
    apt-get install -y $DE_PACKAGES && \
    # \
    # Install additional apps \
    apt-get install -y --no-install-recommends $ADDITIONAL_PACKAGES && \
    # \
    # Clean \
    apt-get autoremove -y && \
    apt-get clean && \
    rm -rf \
        /var/lib/apt/lists/* \
        /var/tmp/* \
        /tmp/*

#
# Replace launch scripts
COPY --chmod=777 scripts/launch-comp.sh scripts/startup.sh scripts/start-plasma.sh /opt/gow/
COPY --chmod=777 scripts/startdbus.sh /opt/gow/startdbus

#
# Fix locals
COPY scripts/locale /etc/default/locale

#
# Allow anyone to start dbus without password
RUN echo "\nALL ALL=NOPASSWD: /opt/gow/startdbus" >> /etc/sudoers

#
# Fix bwarp perms for flatpaks
RUN chmod u+s /usr/bin/bwrap

ARG IMAGE_SOURCE
LABEL org.opencontainers.image.source=$IMAGE_SOURCE
