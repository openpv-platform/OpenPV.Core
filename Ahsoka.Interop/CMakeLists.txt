# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.8)

project ("Ahsoka.Interop")

add_subdirectory ("Ahsoka.Proto")

#Allow Resource Embedding
include(Functions.cmake)

# Interop for C++ SDK
IF (NOT WIN32)
    add_subdirectory ("Ahsoka.Core")
    add_subdirectory ("Ahsoka.Core.Test")
ENDIF()

#Interop Objects for Services
IF (NOT WIN32)
    add_subdirectory ("Ahsoka.WiFi.ST")
ENDIF()

# MOVE Outputs to Normal Location so not in Ahsoka Output.
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_OUTPUTS})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_OUTPUTS})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_OUTPUTS})

IF (NOT WIN32)
    add_subdirectory ("ThirdParty/tiny-process-library")

    FILE(COPY SDK_CMakeLists.txt DESTINATION ${AHSOKA_BUILD_OUTPUTS}/)
    FILE(RENAME ${AHSOKA_BUILD_OUTPUTS}/SDK_CMakeLists.txt ${AHSOKA_BUILD_OUTPUTS}/CMakeLists.txt)
ENDIF()

# Add Cairo Interop build
IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")
    add_subdirectory ("Ahsoka.Drawing.OpenView")
    add_subdirectory ("Ahsoka.VideoPlayer")
ENDIF()
IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinuxPro_TargetSupport/SDK$")
    add_subdirectory ("Ahsoka.LayerManager")
    add_subdirectory ("Ahsoka.Drawing.OpenView")
    add_subdirectory ("Ahsoka.VideoPlayer") 
ENDIF()



