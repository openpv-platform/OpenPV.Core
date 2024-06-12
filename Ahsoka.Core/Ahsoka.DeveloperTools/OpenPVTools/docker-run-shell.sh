#! /bin/sh
docker run -it --privileged -v $LOCAL_PATH:/projects -e DISPLAY=unix$DISPLAY $PULSE_SERVER_TCP_ENV  --name $CONTAINER_NAME $IMAGE_NAME $ENTRY_POINT
