# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

# Set up project target
project ("Ahsoka.Cairo.GLES" VERSION 1.0 DESCRIPTION "CairoSharp Interop for creating a Cairo GLES context")

# Set the Linker flags that will link neccesary 3rd party libraries into our shared lib
set(CMAKE_SHARED_LINKER_FLAGS "-lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo -lpthread")
include_directories( Cairo EGL Wayland Touch )

# add the target Library
add_library(Ahsoka.Cairo.GLES SHARED
            OpenPvGLESContext.c
            EGL/EGLSurface.c
            Cairo/CairoContext.c
            Wayland/WaylandClientInterop.c
            Wayland/xdg-shell-client-protocol.c
            Touch/XdgTouch.c)

set_target_properties("Ahsoka.Cairo.GLES" PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties("Ahsoka.Cairo.GLES" PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties("Ahsoka.Cairo.GLES" PROPERTIES PREFIX "")


# Link 3rd party libraries into executable target
set(CMAKE_EXE_LINKER_FLAGS " -lwayland-server -lwayland-client -lEGL -lGLESv2 -lcairo")

# Set the executable target
add_executable(GLESContextTests Tests/SharedLibTests.c)
target_link_libraries(GLESContextTests Ahsoka.Cairo.GLES)