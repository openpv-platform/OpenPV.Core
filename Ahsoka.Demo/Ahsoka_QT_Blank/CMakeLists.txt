cmake_minimum_required(VERSION 3.5)

project(Ahsoka.QT.Blank LANGUAGES CXX)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -pthread")

#Sets build output to source directory
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)

#Output executable links to local library
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,-rpath='$ORIGIN'")

find_package(Qt5 COMPONENTS Widgets REQUIRED)

FILE(GLOB Headers *.h)
FILE(GLOB Sources *.cpp)
FILE(GLOB Forms *.ui)
FILE(GLOB Resources *.qrc)

#Add Ahsoka Library Subdirectory
add_subdirectory ("Ahsoka.Client.Shared/$ENV{AhsokaPlatform}/SDK")

add_executable(Ahsoka.QT.Blank
    ${Sources}
    ${Forms}
    ${Resources}
    ${AhsokaSources}
)



add_ahsoka(Ahsoka.QT.Blank)

# Required to Deploy to the Target
install(DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs/ DESTINATION Ahsoka.QT.Blank
    PATTERN "**/*"
              PERMISSIONS OWNER_READ OWNER_EXECUTE OWNER_WRITE
    )
