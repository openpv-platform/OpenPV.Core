# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

set(PROJECT_NAME "Ahsoka.LayerManager.OpenViewLinuxPro")
set(CMAKE_EXE_LINKER_FLAGS "-lilmControl -lilmCommon -lwayland-client -livi-application ")
set(CMAKE_SHARED_LINKER_FLAGS "-lilmControl -lilmCommon -lwayland-client -livi-application ")
add_definitions( -DISPRO)

# Set up project target
project (${PROJECT_NAME} VERSION 1.0 DESCRIPTION "OpenPV Layer Manager Service")

# Set the Linker flags that will link neccesary 3rd party libraries into our shared lib
include_directories(Wayland Touch)

# add the target Library
add_executable(${PROJECT_NAME} LayerManager.c)

set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Apps/OpenViewLinuxPro_TargetSupport)
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Apps/OpenViewLinuxPro_TargetSupport)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")

