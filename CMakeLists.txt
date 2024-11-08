# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.8)

project ("Ahsoka.Platform")

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

IF (WIN32)

    # Disable Warnings for Proto about exporting and C17 Deprecicated Build Warnings
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /wd4251 /D _SILENCE_ALL_CXX17_DEPRECATION_WARNINGS") 

    set(CMAKE_CXX_FLAGS_RELEASE "/MT")
    set(CMAKE_CXX_FLAGS_DEBUG "/MTd")
    
ELSE()

    # Include sub-projects.
    set(CMAKE_TOOLCHAIN_FILE ${OE_CMAKE_TOOLCHAIN_FILE})
    set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/lib)
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/lib)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/bin)

    SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -pthread -Wno-psabi")

ENDIF()


add_subdirectory ("Ahsoka.Interop")