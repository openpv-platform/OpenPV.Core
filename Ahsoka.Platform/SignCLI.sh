#!/bin/bash
PATH_NAME="../../PlatformSupport/MacOSARM64_TargetSupport"
ENTITLEMENTS="../MyAppEntitlements.entitlements"
SIGNING_IDENTITY="Developer ID Application: Enovation Controls LLC (C28LL293G6)"

#NOTE that the Distribution Cert must also be installed on the machine.

find "$PATH_NAME"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done
