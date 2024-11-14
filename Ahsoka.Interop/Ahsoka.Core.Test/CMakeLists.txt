# CMakeList.txt : CMake project for Ahsoka.System.Client, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

project (Ahsoka.Core.Test)

FILE(GLOB Service Services/*.cpp)

add_executable(Ahsoka.Core.Test main.cpp)
target_include_directories(Ahsoka.Core.Test PUBLIC Services)
target_link_libraries(Ahsoka.Core.Test PRIVATE Ahsoka.Core Ahsoka.Proto)

#Uncomment to use Custom Service.json
FILE(COPY ServiceConfiguration.json DESTINATION ${AHSOKA_BUILD_OUTPUTS}/bin)
