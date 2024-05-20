#!/bin/bash
export WORK_DIR=/mnt/c/projects
docker run --privileged -it -v $WORK_DIR:/home/openviewpro -v /dev:/dev -v /media/:/media/ -w /home/openviewpro --name OpenViewProSDK openpv/openviewpro_sdk_3_0

