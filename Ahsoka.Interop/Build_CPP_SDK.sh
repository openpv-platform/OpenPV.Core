#!/bin/bash

cd /workspaces/Ahsoka.Platform

UBUNTU_BUILD_TYPE="Debug"
OPENST_BUILD_TYPE="Release"

# Clean Build Outputs
AHSOKA_BUILD_ROOT="/workspaces/Ahsoka.Core/Build"
AHSOKA_BUILD_OUTPUT_ROOT="/workspaces/Ahsoka.Core/BuildOutputs/SDK"
mkdir -p $AHSOKA_BUILD_ROOT
mkdir -p $AHSOKA_BUILD_OUTPUT_ROOT

# Build Output Directory
AHSOKA_BUILD="$AHSOKA_BUILD_ROOT/Ubuntu64"
AHSOKA_BUILD_OUTPUT="$AHSOKA_BUILD_OUTPUT_ROOT/Ubuntu64_TargetSupport/SDK"
rm -rf $AHSOKA_BUILD $AHSOKA_BUILD_OUTPUT

# Configure and Build for Ubuntu
/usr/bin/cmake -DCMAKE_BUILD_TYPE=$UBUNTU_BUILD_TYPE -DAHSOKA_BUILD_OUTPUTS=$AHSOKA_BUILD_OUTPUT -S/workspaces/Ahsoka.Core -B$AHSOKA_BUILD -G Ninja
/usr/bin/cmake --build $AHSOKA_BUILD

#Delete Build Directory
AHSOKA_BUILD="$AHSOKA_BUILD_ROOT/OpenViewLinux"
AHSOKA_BUILD_OUTPUT="$AHSOKA_BUILD_OUTPUT_ROOT/OpenViewLinux_TargetSupport/SDK"
rm -rf $AHSOKA_BUILD $AHSOKA_BUILD_OUTPUT

# Configure SDK for OpenLinux
source /root/sdk/environment-setup-cortexa7t2hf-neon-vfpv4-ostl-linux-gnueabi

# Turn off -g on Release Builds.
if [ $OPENST_BUILD_TYPE="Release" ]
then
    CXXFLAGS=" -O2 -pipe -feliminate-unused-debug-types"
    OE_QMAKE_CXXFLAGS=$CXXFLAGS
    OE_QMAKE_CFLAG=$CXXFLAGS
    CFLAGS=$CXXFLAGS  
fi

# Create new Build Folder
mkdir -p $AHSOKA_BUILD

/usr/bin/cmake -DCMAKE_BUILD_TYPE=$OPENST_BUILD_TYPE -DAHSOKA_BUILD_OUTPUTS=$AHSOKA_BUILD_OUTPUT -DCMAKE_TOOLCHAIN_FILE=/root/sdk/sysroots/x86_64-ostl_sdk-linux/usr/share/cmake/OEToolchainConfig.cmake -S/workspaces/Ahsoka.Core -B$AHSOKA_BUILD -G Ninja
/usr/bin/cmake --build $AHSOKA_BUILD
