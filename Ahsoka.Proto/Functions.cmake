####################################################################################################
# This function removes platform specific tags from proto files
# Example:
# - input file: example.proto
# - output file: edited.proto
# remove_protobuf_net("example.proto" "edited.proto")
####################################################################################################
function(remove_protobuf_net source_file destination_file)
    if(EXISTS "${destination_file}")
        if("${destination_file}" IS_NEWER_THAN "${source_file}")
            return()
        endif()
    endif()

    file(READ "${source_file}" text)

    #remove 'import "protobuf-net/protogen.proto"';
    string(REGEX REPLACE "import \"protobuf-net[^;]+;" "" text "${text}")

    #remove '(.protobuf_net.enumvalopt).name = "GetIPInfo"]' or ', (.protobuf_net.fieldopt).name = "OverrideMACAddress"'
    string(REGEX REPLACE "(, *)?\\(\\.protobuf_net[^],]+" "" text "${text}")

    #remove '[]' or '[ ]'
    string(REGEX REPLACE "\\[ *\\] *;" ";" text "${text}")

    file(WRITE "${destination_file}" "${text}")
endfunction()