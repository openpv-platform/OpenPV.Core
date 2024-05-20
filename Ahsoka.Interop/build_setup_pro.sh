#!/bin/bash

mkdir -p /workspaces/Ahsoka.Platform/Ahsoka.Interop
rsync -a /source/Ahsoka.Interop/ /workspaces/Ahsoka.Platform/Ahsoka.Interop/

mkdir -p /workspaces/Ahsoka.Platform/Ahsoka.Proto
rsync -a /source/Ahsoka.Proto/ /workspaces/Ahsoka.Platform/Ahsoka.Proto/

mkdir -p /workspaces/Ahsoka.Platform/Ahsoka.Platform/Ahsoka.Core/Resources
rsync -a /source/Ahsoka.Platform/Ahsoka.Core/Resources/ /workspaces/Ahsoka.Platform/Ahsoka.Platform/Ahsoka.Core/Resources/

cp /source/CmakeLists.txt /workspaces/Ahsoka.Platform/CMakeLists.txt
cp /source/CmakePresets.json /workspaces/Ahsoka.Platform/CMakePresets.json

cd /workspaces/Ahsoka.Platform
chmod 777 -R .

./Ahsoka.Interop/Build_CPP_SDK_PRO.sh

cp -r /workspaces/Ahsoka.Platform/BuildOutputs /source
