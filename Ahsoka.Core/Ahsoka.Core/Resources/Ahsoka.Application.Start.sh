#!/bin/sh
echo "App Start Time" $(date +%T.%3N)

. /etc/profile.d/app_profile.sh

cd ${ApplicationFolder}

if [ -f ../app_env.sh ];
then
    . ../app_env.sh
fi

if [ -f ./start_ui_helper.sh ];
then
    ./start_ui_helper.sh
fi

${PreReleaseScript}

chmod +x ./${ExecCommand}
./${ExecCommand} &

${PostReleaseScript}