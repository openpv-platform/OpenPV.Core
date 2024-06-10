# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

# Link 3rd party libraries into executable target
IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")
    set(PROJECT_NAME "Ahsoka.Cairo.OpenView")
    set(CMAKE_EXE_LINKER_FLAGS " -lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo")
    set(CMAKE_SHARED_LINKER_FLAGS "-lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo -lpthread")
ENDIF()
IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinuxPro_TargetSupport/SDK$")
    set(PROJECT_NAME "Ahsoka.Cairo.OpenViewPro")
    set(CMAKE_EXE_LINKER_FLAGS " -lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo -lwayland-egl")
    set(CMAKE_SHARED_LINKER_FLAGS "-lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo -lpthread -lwayland-egl")
ENDIF()

# Set up project target
project (${PROJECT_NAME} VERSION 1.0 DESCRIPTION "CairoSharp Interop for creating a Cairo GLES context")

# Set the Linker flags that will link neccesary 3rd party libraries into our shared lib
include_directories( Cairo EGL Wayland Touch )

# add the target Library
add_library(${PROJECT_NAME} SHARED
            OpenPvGLESContext.c
            EGL/EGLSurface.c
            Cairo/CairoContext.c
            Wayland/WaylandClientInterop.c
            Wayland/xdg-shell-client-protocol.c
            Touch/XdgTouch.c)

set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")




# Set the executable target
add_executable(GLESContextTests Tests/SharedLibTests.c)
target_link_libraries(GLESContextTests ${PROJECT_NAME})