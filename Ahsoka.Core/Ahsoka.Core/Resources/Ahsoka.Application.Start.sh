#!/bin/sh
echo "App Start Time" $(date +%T.%3N)

. /etc/profile.d/app_profile.sh

cd ${ApplicationFolder}

DIR=".appinfo/start"
if [ -d "$DIR" ]; then
	for FILE in "$DIR"/*; do
		if [ -f "$FILE" ]; then	    
			. "$FILE"
		fi
	done
fi

${PreReleaseScript}

chmod +x ./${ExecCommand}
./${ExecCommand} &

${PostReleaseScript}