#!/bin/bash

#Copy Interop Folder (Main Build)
mkdir -p /workspaces/Ahsoka.Platform/Ahsoka.Interop
rsync -a /source/Ahsoka.Interop/ /workspaces/Ahsoka.Platform/Ahsoka.Interop/

# Add the Proto Files from Core
mkdir -p /workspaces/Ahsoka.Platform/Ahsoka.Core/Ahsoka.Core/Proto
rsync -a /source/Ahsoka.Core/Ahsoka.Core/Proto/ /workspaces/Ahsoka.Platform/Ahsoka.Core/Ahsoka.Core/Proto/
cp /source/Ahsoka.Shared/Services.proto /workspaces/Ahsoka.Platform/Ahsoka.Core/Ahsoka.Core/Proto/Services.proto #  now in Shared

# Add the Resources from Core
mkdir -p /workspaces/Ahsoka.Core/Ahsoka.Core/Ahsoka.Core/Resources
rsync -a /source/Ahsoka.Core/Ahsoka.Core/Resources/ /workspaces/Ahsoka.Platform/Ahsoka.Core/Ahsoka.Core/Resources/

cp /source/CmakePresets.json /workspaces/Ahsoka.Platform/CMakePresets.json
cp /source/CMakeLists.txt /workspaces/Ahsoka.Platform/CMakeLists.txt

cd /workspaces/Ahsoka.Platform
chmod 777 -R .

./Ahsoka.Interop/Build_CPP_SDK.sh

cp -r /workspaces/Ahsoka.Platform/BuildOutputs /source
