# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")
    set(PROJECT_NAME "Ahsoka.VideoPlayer.OpenViewLinux")
ENDIF()

IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinuxPro_TargetSupport/SDK$")
    set(PROJECT_NAME "Ahsoka.VideoPlayer.OpenViewLinuxPro")
ENDIF()

find_package(PkgConfig REQUIRED)
pkg_check_modules(gtk3 REQUIRED IMPORTED_TARGET gtk+-3.0)
pkg_search_module(gstreamer REQUIRED IMPORTED_TARGET gstreamer-1.0>=1.4)
pkg_search_module(gstreamer-sdp REQUIRED IMPORTED_TARGET gstreamer-sdp-1.0>=1.4)
pkg_search_module(gstreamer-app REQUIRED IMPORTED_TARGET gstreamer-app-1.0>=1.4)
pkg_search_module(gstreamer-video REQUIRED IMPORTED_TARGET gstreamer-video-1.0>=1.4)

# Set up project target
project (${PROJECT_NAME} VERSION 1.0 DESCRIPTION "Video Player for OpenPV")

# add the target Library
add_executable(${PROJECT_NAME} Ahsoka.VideoPlayer.c)

target_link_libraries(${PROJECT_NAME}
    PkgConfig::gtk3
    PkgConfig::gstreamer
    PkgConfig::gstreamer-sdp
    PkgConfig::gstreamer-app
    PkgConfig::gstreamer-video
)

set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")
