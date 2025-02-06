#!/bin/bash

#Copy Interop Folder (Main Build)
mkdir -p /workspaces/Ahsoka.Core/Ahsoka.Interop
rsync -a /source/Ahsoka.Interop/ /workspaces/Ahsoka.Core/Ahsoka.Interop/

cp /source/CmakePresets.json /workspaces/Ahsoka.Core/CMakePresets.json
cp /source/CMakeLists.txt /workspaces/Ahsoka.Core/CMakeLists.txt

cd /workspaces/Ahsoka.Core
chmod 777 -R .

./Ahsoka.Interop/Build_CPP_SDK_PRO.sh

cp -r /workspaces/Ahsoka.Core/BuildOutputs /source
