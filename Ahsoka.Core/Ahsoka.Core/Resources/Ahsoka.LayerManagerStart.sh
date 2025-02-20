#!/bin/sh
echo "Layer Manager Start Time" $(date +%T.%3N)
. /etc/profile.d/app_profile.sh

sleep 4

chmod +x /usr/local/Ahsoka/factory/weston/Ahsoka.LayerManager
/usr/bin/stdbuf -oL /usr/local/Ahsoka/factory/weston/Ahsoka.LayerManager &
