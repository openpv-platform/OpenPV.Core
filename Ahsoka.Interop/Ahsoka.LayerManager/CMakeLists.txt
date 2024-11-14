# CMakeList.txt : CairoSharp interop build. 
#
# Ahsoka.GLES

cmake_minimum_required (VERSION 3.8)

include(CheckCCompilerFlag)
check_c_compiler_flag(-Wdeprecated-declarations HAVE_DEPRECATED_DECLARATIONS)

set(PROJECT_NAME "Ahsoka.LayerManager")
set(CMAKE_EXE_LINKER_FLAGS "-lilmControl -lilmCommon -lwayland-client -livi-application -labsl_log_internal_check_op -labsl_log_internal_message")
set(CMAKE_SHARED_LINKER_FLAGS "-lilmControl -lilmCommon -lwayland-client -livi-application -labsl_log_internal_check_op -labsl_log_internal_message")
add_definitions( -DISPRO)

# Set up project target
project (${PROJECT_NAME} VERSION 1.0 DESCRIPTION "OpenPV Layer Manager Service")

# Set the Linker flags that will link neccesary 3rd party libraries into our shared lib
include_directories(Wayland Touch)

# add the target Library
add_executable(${PROJECT_NAME} main.cpp LayerManager.cpp)

 # Add ProtoBuf
find_package (Protobuf REQUIRED)
target_link_libraries(${PROJECT_NAME} PUBLIC protobuf)

# Add Internal Libraries
target_link_libraries(${PROJECT_NAME} PRIVATE Ahsoka.Proto Ahsoka.Core)

# Specify Output Directories
set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS_RELEASE -s)
set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Apps/OpenViewLinuxPro_TargetSupport)
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Apps/OpenViewLinuxPro_TargetSupport)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")

