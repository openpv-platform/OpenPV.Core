#!/bin/bash
APP_NAME="../PlatformSupport/Ahsoka.Tools/MacOS/OpenPV.DeveloperToolkit.app"
ENTITLEMENTS="../MyAppEntitlements.entitlements"
SIGNING_IDENTITY="Developer ID Application: Enovation Controls LLC (C28LL293G6)"

#NOTE that the Distribution Cert must also be installed on the machine.

find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"