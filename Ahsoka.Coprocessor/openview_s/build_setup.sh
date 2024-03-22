#!/bin/bash

mkdir /workspaces/Ahsoka.Coprocessor
mkdir /workspaces/Ahsoka.Proto
mkdir /workspaces/BuildOutputs

rsync -a /source/Ahsoka.Coprocessor/ /workspaces/Ahsoka.Coprocessor/
rsync -a /source/Ahsoka.Proto/ /workspaces/Ahsoka.Proto/

cd /workspaces
chmod 777 -R .

./Ahsoka.Coprocessor/openview_s/build_firmware.sh

mkdir -p /source/BuildOutputs/Firmware/OpenViewLinux_TargetSupport/
cp ./BuildOutputs/Firmware/OpenViewLinux_TargetSupport/firmware_CM4.elf /source/BuildOutputs/Firmware/OpenViewLinux_TargetSupport/firmware_CM4.elf
