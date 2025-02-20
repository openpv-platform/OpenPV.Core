#!/bin/sh

echo "Running custom init."

# weston environment variables when weston is launched pre-systemd
export XDG_RUNTIME_DIR=/tmp/xdg-runtime-dir
export WAYLAND_DISPLAY="wayland-1"
export WESTON_USER=root
export WL_EGL_GBM_FENCE=0
export XDG_CONFIG_DIRS=/usr/local/Ahsoka/current:/usr/local/Ahsoka/factory:/etc/xdg/weston

# Needed for Xwayland
export DISPLAY=:0

# udevd may be necessary for touchscreen
udevd &
UDEV=$!

# Prep all needed weston mount points
mount -t proc none /proc
mount -t sysfs none /sys
mount -t tmpfs -o size=47404k,rw,nosuid,nodev,relatime,size=47404k,mode=700,uid=1000,gid=1000 tmpfs /tmp

mkdir -p /dev/pts
mount -t devpts -o mode=0620,gid=5 devpts /dev/pts

mkdir -p ${XDG_RUNTIME_DIR}
chmod 0700 ${XDG_RUNTIME_DIR}

modprobe -a tidss pvrsrvkm sii902x display_connector panel_simple

# Sleep so /dev/dri/card1 populates
/bin/sleep 0.02


ARGS="--modules=systemd-notify.so"
ARGS="$ARGS --backend=drm-backend.so"
ARGS="$ARGS --current-mode"
#ARGS="$ARGS --config=/etc/xdg/weston/weston.ini"
ARGS="$ARGS --drm-device=card1"
ARGS="$ARGS --continue-without-input"
ARGS="$ARGS --flight-rec-scopes="
ARGS="$ARGS --logger-scopes="

/usr/bin/weston $ARGS &

exec /sbin/init

exit