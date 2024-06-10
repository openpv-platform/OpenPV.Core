# CMakeList.txt : CMake project for Ahsoka.System.Client, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

include(CheckCCompilerFlag)
check_c_compiler_flag(-Wdeprecated-declarations HAVE_DEPRECATED_DECLARATIONS)

#Include the Service Configuration
embed_resource("../../Ahsoka.Platform/Ahsoka.Core/Resources/OPVServicesConfiguration.json" "ServiceFramework/ServiceConfiguration.hpp" "ServiceConfig")

# Add source to this project's executable.
FILE(GLOB ServiceFramework ServiceFramework/*.cpp)
FILE(GLOB Service Services/*.cpp)

# Define Library (.dll - static for windows .so - shared) for linux)
IF (WIN32)
        add_Library(Ahsoka.Client.Shared STATIC ${ServiceFramework} ${Service} )
ELSE()
        add_Library(Ahsoka.Client.Shared SHARED ${ServiceFramework} ${Service} )
ENDIF()

# Add Included Folders.
target_include_directories(Ahsoka.Client.Shared PUBLIC Services ServiceFramework)

# Disable compiler warnings
IF (HAVE_DEPRECATED_DECLARATIONS)
    target_compile_options(Ahsoka.Client.Shared PUBLIC -Wno-deprecated-declarations)
ENDIF()

# Add Internal Libraries
find_library( AHSOKA_LIBS NAMES Ahsoka.Proto)
target_link_libraries(Ahsoka.Client.Shared PRIVATE Ahsoka.Proto)

find_library( TINY_LIB NAMES tiny-process-library)
target_link_libraries(Ahsoka.Client.Shared PRIVATE tiny-process-library)

IF (WIN32)
       
        #Find Proto in vcpkg
        find_package(Protobuf CONFIG REQUIRED)
        target_link_libraries(Ahsoka.Client.Shared PRIVATE protobuf::libprotoc protobuf::libprotobuf protobuf::libprotobuf-lite)

        find_package(ZeroMQ CONFIG REQUIRED)
        target_link_libraries(Ahsoka.Client.Shared PRIVATE libzmq-static)

        # Find zmq from vcconfig
        find_path(CPPZMQ_INCLUDE_DIRS "zmq.hpp")
        target_include_directories(Ahsoka.Client.Shared PRIVATE ${CPPZMQ_INCLUDE_DIRS})
      
ELSE()
        # Add ProtoBuf and zero mq as well as our Proto Compiled Outputs
        find_package (Protobuf REQUIRED)
        target_link_libraries(Ahsoka.Client.Shared PUBLIC protobuf)

        #Add Zero MQ
        find_library (ZeroMQ NAMES zmq REQUIRED)
        target_link_libraries(Ahsoka.Client.Shared PUBLIC zmq)
ENDIF()

# Copy the Services headers to the Output Folder
FILE(GLOB ServiceHeaders Services/*.h )
FILE(COPY ${ServiceHeaders} DESTINATION ${AHSOKA_BUILD_OUTPUTS}/inc/services)

FILE(GLOB ServiceFrameworkHeaders ServiceFramework/*.h )
FILE(COPY ${ServiceFrameworkHeaders} DESTINATION ${AHSOKA_BUILD_OUTPUTS}/inc/services)

#Pick up Proto Headers and Include in Output Folder
ADD_CUSTOM_COMMAND(TARGET Ahsoka.Client.Shared
        POST_BUILD
        COMMAND echo Create Proto Header Folder 
        COMMAND ${CMAKE_COMMAND} -E make_directory ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/Configuration.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/DataService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/BLEService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/InstallService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/IOService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/IotService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/NetworkService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/SparkService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/Services.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/SystemService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/CanService.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
        COMMAND ${CMAKE_COMMAND} -E copy ${PROTO_HEADER_DIR}/CanConfiguration.pb.h ${AHSOKA_BUILD_OUTPUTS}/inc/messages
)
