# CMakeList.txt : CMake project for Ahsoka.System.Client, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

project (Ahsoka.Client.Test)

add_executable(Ahsoka.Client.Test main.cpp)
target_link_libraries(Ahsoka.Client.Test Ahsoka.Client.Shared Ahsoka.Proto)

#Uncomment to use Custom Service.json
FILE(COPY ServiceConfiguration.json DESTINATION ${AHSOKA_BUILD_OUTPUTS}/bin)
