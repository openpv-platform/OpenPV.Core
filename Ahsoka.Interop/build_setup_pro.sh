#!/bin/bash

#Copy Interop Folder (Main Build)
mkdir -p /workspaces/Ahsoka.Core/Ahsoka.Interop
rsync -a /source/Ahsoka.Interop/ /workspaces/Ahsoka.Core/Ahsoka.Interop/

# Add the Proto Files from Core
mkdir -p /workspaces/Ahsoka.Core/Ahsoka.Shared
rsync -a /source/Ahsoka.Shared /workspaces/Ahsoka.Core/Ahsoka.Shared
cp /source/Ahsoka.Shared/Configuration.proto /workspaces/Ahsoka.Core/Ahsoka.Shared/Configuration.proto #  now in Shared
cp /source/Ahsoka.Shared/Services.proto /workspaces/Ahsoka.Core/Ahsoka.Shared/Services.proto #  now in Shared

# Add the Resources from Core
mkdir -p /workspaces/Ahsoka.Core/Ahsoka.Core/Ahsoka.Core/Resources
rsync -a /source/Ahsoka.Core/Ahsoka.Core/Resources/ /workspaces/Ahsoka.Core/Ahsoka.Core/Ahsoka.Core/Resources/

cp /source/CmakePresets.json /workspaces/Ahsoka.Core/CMakePresets.json
cp /source/CMakeLists.txt /workspaces/Ahsoka.Core/CMakeLists.txt

cd /workspaces/Ahsoka.Core
chmod 777 -R .

./Ahsoka.Interop/Build_CPP_SDK_PRO.sh

cp -r /workspaces/Ahsoka.Core/BuildOutputs /source
