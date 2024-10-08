# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.8)

project (Ahsoka.Services)

IF (${AHSOKA_BUILD_OUTPUTS} MATCHES "OpenViewLinux_TargetSupport/SDK$")

    include_directories( .  ./Shared ./OpenViewLinux)
        
    set(sources ${sources}  "./OpenViewLinux/PlatformInterface.cpp" 
                            "./OpenViewLinux/PlatformInterfaceST.cpp" 
                            "./OpenViewLinux/type-c-role-switch.cpp" 
                            "./Shared/Ahsoka.Services.cpp"  )

    add_Library(Ahsoka.Services SHARED ${sources})

    set_target_properties(Ahsoka.Services PROPERTIES OUTPUT_NAME Ahsoka.Services.OpenViewLinux)

    set_target_properties(Ahsoka.Services PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
    set_target_properties(Ahsoka.Services PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS}/../../../Ahsoka.Services/)
    set_target_properties(Ahsoka.Services PROPERTIES PREFIX "")

ENDIF()

