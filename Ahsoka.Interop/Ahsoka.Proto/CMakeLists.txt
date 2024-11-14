# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.8)

set(CMAKE_AUTOUIC OFF)
set(CMAKE_AUTOMOC OFF)
set(CMAKE_AUTORCC OFF)

project ("Ahsoka.Proto" LANGUAGES CXX)

include(Functions.cmake)

find_package(Protobuf REQUIRED)

message(STATUS "Using Protocol Buffers ${Protobuf_VERSION}")

set(PROTOBUF_DEFINITION_FILES "${CMAKE_SOURCE_DIR}/Ahsoka.Shared/Configuration.proto;${CMAKE_SOURCE_DIR}/Ahsoka.Shared/Services.proto" ) # Note that protobuf_generate_cpp does not support subdirectories
message(PROTOBUF_DEFINITION_FILES  ${PROTOBUF_DEFINITION_FILES})

foreach(PROTO_FILE ${PROTOBUF_DEFINITION_FILES})
    file(RELATIVE_PATH PROTO_RELATIVE_FILE ${CMAKE_CURRENT_SOURCE_DIR} ${PROTO_FILE})
    set(PROTOBUF_PREPROCESSED_FILE ${CMAKE_CURRENT_BINARY_DIR}/${PROTO_RELATIVE_FILE})

    remove_protobuf_net("${PROTO_FILE}" "${PROTOBUF_PREPROCESSED_FILE}")
    list(APPEND PROTOBUF_PREPROCESSED_FILES ${PROTOBUF_PREPROCESSED_FILE})
endforeach()

list(APPEND PROTOBUF_IMPORT_DIRS ${PROTOBUF_INCLUDE_DIR})  # Only necessary for Windows
protobuf_generate_cpp(PROTO_SRC PROTO_HEADER ${PROTOBUF_PREPROCESSED_FILES})

add_Library(Ahsoka.Proto OBJECT ${PROTO_SRC})
target_include_directories(Ahsoka.Proto PUBLIC ${PROTO_HEADER_DIR} ${PROTOBUF_INCLUDE_DIR} ${CMAKE_CURRENT_BINARY_DIR})
include_directories(${PROTO_HEADER_DIR} ${PROTOBUF_INCLUDE_DIR} ${CMAKE_CURRENT_BINARY_DIR})

# Set this so we an copy them to output later when the library target is built.
set(PROTO_HEADER_DIR ${CMAKE_CURRENT_BINARY_DIR} PARENT_SCOPE)
