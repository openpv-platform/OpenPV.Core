#!/bin/sh
echo "Installer Start Time" $(date +%T.%3N)
. /etc/profile.d/app_profile.sh

if [ -f ${FieldInstallerFolder}/${ExecCommand} ];
then
    cd ${FieldInstallerFolder}
else
    cd ${FactoryInstallerFolder}
fi

if [ -f ../../app_env.sh ];
then
    . ../../app_env.sh
fi

chmod +x ./${ExecCommand}
./${ExecCommand} &