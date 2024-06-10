cmake_minimum_required(VERSION 3.5)

project(Ahsoka.QT.CAN LANGUAGES CXX)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -pthread -Wno-psabi")

#Sets build output to source directory
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)

#Output executable links to local library
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,-rpath='$ORIGIN'")

find_package(Qt5 COMPONENTS Widgets REQUIRED)

FILE(GLOB Headers *.h)
FILE(GLOB Sources *.cpp *.hpp)
FILE(GLOB Forms *.ui)
FILE(GLOB Resources *.qrc)

# This Command runs the Ahsoka CAN Code Generator to Generate CAN Model Objects
# Note we also copy the Can Calibration File to the Command Line Folder
# for use in debugging on the Developer Machine (Ubuntu)
add_custom_command(
  OUTPUT ${CMAKE_CURRENT_SOURCE_DIR}/Generated/CANModels.generated.h ${CMAKE_CURRENT_SOURCE_DIR}/Generated/CANModels.generated.cpp
  COMMAND ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/Ubuntu64/CommandLine/Ahsoka.CommandLine --GenerateCANClasses ${CMAKE_SOURCE_DIR}/OpenLinuxST.QT.PackageInfo.json ${CMAKE_SOURCE_DIR}/Generated/CANModels.generated.h "DEMO_CAN" "CanViewModelBase" "Cpp"
  COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_SOURCE_DIR}/STDemoPackage.1.cancalibration.json ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/$ENV{AhsokaPlatform}/CommandLine/
  DEPENDS ${SOURCE_FILES} ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/Ubuntu64/CommandLine/Ahsoka.CommandLine main.cpp
  WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
)

# CREATE Executable called Ahsoka.QT.CAN
add_executable(Ahsoka.QT.CAN
    ${Sources}
    ${Forms}
    ${Resources}
    ${CMAKE_CURRENT_SOURCE_DIR}/Generated/CANModels.generated.cpp
)

#Add Ahsoka Library Subdirectory
add_subdirectory ("Ahsoka.Client.Shared/$ENV{AhsokaPlatform}/SDK")
add_ahsoka(Ahsoka.QT.CAN)

# Required to Deploy to the Target
install(DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs/ DESTINATION Ahsoka.QT.CAN
    PATTERN "**/*"
              PERMISSIONS OWNER_READ OWNER_EXECUTE OWNER_WRITE
    )
