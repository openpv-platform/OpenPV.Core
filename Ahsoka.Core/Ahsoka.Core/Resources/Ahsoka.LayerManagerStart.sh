#!/bin/sh
echo "Layer Manager Start Time" $(date +%T.%3N)
. /etc/profile.d/app_profile.sh

sleep 1

/usr/bin/stdbuf -oL /usr/bin/layer-add-surfaces -l 1000 -s 1000 &
