# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.8)

project (Ahsoka.WiFi.OpenViewLinux)

IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")

    add_custom_target(Ahsoka.WiFi.OpenViewLinux ALL COMMAND make WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} )
    
ENDIF()

