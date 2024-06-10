# DotNet 
export DOTNET_ROOT=/usr/local/Ahsoka/dependency/dotnet.7.0
export PATH=$PATH:$DOTNET_ROOT

export XDG_RUNTIME_DIR=/tmp/xdg-runtime-dir
export WAYLAND_DISPLAY="wayland-1"
export WESTON_USER=root
export WL_EGL_GBM_FENCE=0

# QT webkit environment files
export QT_QPA_PLATFORM=wayland-egl
export QT_QPA_EGLFS_ALWAYS_SET_MODE='1'
export QML2_IMPORT_PATH='/usr/lib/qt5/qml/'
export QT_QPA_EGLFS_KMS_CONFIG='/etc/eglfs_config.json'
export QT_QPA_FONTDIR='/usr/share/fonts/ttf'