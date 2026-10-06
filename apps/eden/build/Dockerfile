ARG BASE_APP_IMAGE=ghcr.io/games-on-whales/base-app:edge
FROM ${BASE_APP_IMAGE}

# Eden (https://git.eden-emu.dev/eden-emu/eden) — Switch emulator.
# Only the emulator is shipped here; prod.keys, firmware and game files are
# provided by the user via a read-only host mount at /switch (see
# scripts/startup.sh and the Heeler app config).
ARG EDEN_VERSION=v0.2.1
# PGO build is the recommended one (~10-30% faster than standard)
ARG EDEN_BUILD=amd64-clang-pgo

# The Linux AppImage is fully self-contained: statically linked binary plus
# bundled Qt6, FFmpeg and mesa Vulkan ICDs (AMD/Intel/Nouveau). We extract it
# at build time so no FUSE is needed at runtime. On NVIDIA hosts the
# driver's own Vulkan ICD (exposed by Heeler) takes precedence over the
# bundled mesa ones.
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends wget; \
    wget -q -O /tmp/eden.AppImage \
        "https://stable.eden-emu.dev/${EDEN_VERSION}/Eden-Linux-${EDEN_VERSION}-${EDEN_BUILD}.AppImage"; \
    chmod +x /tmp/eden.AppImage; \
    cd /opt; \
    /tmp/eden.AppImage --appimage-extract; \
    mv /opt/squashfs-root /opt/eden; \
    rm -f /tmp/eden.AppImage; \
    rm -rf /var/lib/apt/lists/*

# Sanity check: the real emulator must be present and statically linked
RUN test -x /opt/eden/shared/bin/eden

COPY --chmod=755 scripts/* /opt/gow/
COPY --chmod=755 cont-init.d/ /etc/cont-init.d/
