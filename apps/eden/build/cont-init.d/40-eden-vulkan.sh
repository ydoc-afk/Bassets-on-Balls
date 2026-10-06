#!/bin/bash
# The NVIDIA container toolkit injects the driver's Vulkan ICD at /etc/vulkan/icd.d/nvidia_icd.json, but the base
# image (and some loaders) only look in /usr/share/vulkan/icd.d. Link it there so Eden finds the NVIDIA driver
# instead of silently falling back to a mesa software ICD. Runs as root during container init.
source /opt/gow/bash-lib/utils.sh

if [ -f /etc/vulkan/icd.d/nvidia_icd.json ] && [ ! -e /usr/share/vulkan/icd.d/nvidia_icd.json ]; then
    mkdir -p /usr/share/vulkan/icd.d
    ln -s /etc/vulkan/icd.d/nvidia_icd.json /usr/share/vulkan/icd.d/nvidia_icd.json
    gow_log "[vulkan] Linked the NVIDIA Vulkan ICD into /usr/share/vulkan/icd.d"
fi
