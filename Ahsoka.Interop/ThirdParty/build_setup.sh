#!/bin/bash

mkdir /esp-hosted
rsync -a /source/esp-hosted/ /esp-hosted/

cd /esp-hosted
chmod 777 -R .

./docker/build_firmware.sh

cp ESPFirmware.zip /source/esp-hosted/ESPFirmware.zip
