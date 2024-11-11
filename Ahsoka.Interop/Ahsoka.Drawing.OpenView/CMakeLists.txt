# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

# Link 3rd party libraries into executable target
IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")
    set(PROJECT_NAME "Ahsoka.Drawing.OpenViewLinux")
    set(CMAKE_EXE_LINKER_FLAGS " -lwayland-server -lwayland-client -lEGL -lGLESv2 ")
    set(CMAKE_SHARED_LINKER_FLAGS "-lwayland-server -lwayland-client -lEGL -lGLESv2 -lpthread")
ENDIF()

IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinuxPro_TargetSupport/SDK$") 
    set(PROJECT_NAME "Ahsoka.Drawing.OpenViewLinuxPro")
    set(CMAKE_EXE_LINKER_FLAGS " -lwayland-server -lwayland-client -lEGL -lGLESv2 -lwayland-egl -livi-application -lilmControl -lilmCommon")
    set(CMAKE_SHARED_LINKER_FLAGS "-lwayland-server -lwayland-client -lEGL -lGLESv2 -lpthread -lwayland-egl -livi-application -lilmControl -lilmCommon")
    add_definitions( -DISPRO)
ENDIF()

# Set up project target
project (${PROJECT_NAME} VERSION 1.0 DESCRIPTION "OpenPV Interop for creating a Wayland GLES context")

# Set the Linker flags that will link neccesary 3rd party libraries into our shared lib
include_directories( EGL Wayland Touch)

# add the target Library
add_library(${PROJECT_NAME} SHARED
            OpenPVWindow.c
            EGL/EGLSurface.c
            Wayland/WaylandClientInterop.c
            Wayland/xdg-shell-client-protocol.c
            Touch/XdgTouch.c)

set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")

