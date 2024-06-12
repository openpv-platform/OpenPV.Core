# Development Containers {#ContainerDevelopment}

Enovation Controls provides containers for use in quickly developing C++ Code on OpenView Displays that require linux toolchains such as the OpenView Select Series.  Currently we support 2 containers depending on your application goals


**Base Toolchain Container (openpv/openview_sdk)** - This container contains the latest toolchain. By default this container can build Ubuntu or STM32MP1 (OpenView S Series) binaries and enables you to build and test C++ applications (Such OpenGL or GTK applications) on Ubuntu or the Target.   Note that our internal workflow rely on VSCode for C++ Development and uses this container.  This container is available publically in Docker Hub at **openpv/openview_sdk**

**QT Creator UI Container (openpv/openview_qt_creator)** - This container builds on the base container (openpv/openview_sdk) and adds QT Creator 5.x.  It was designed to quickly allow you to build build QT5 Applications for OpenView Displays.   This container was specifically tested to allow QT5 on Windows via WSL.  Instructions for setting up and launching the container on Windows 11 are below. This container is available publically in Docker Hub at **openpv/openview_qt_creator**


    The following commands are tested to run on Ubuntu 22.04 on Windows WSL2 running on Windows 11.


**Install Ubuntu 22.04 as a WSL2 Distribution** - These distributions are available via the Microsoft Store.


**Install Docker Desktop and Configure it to use WSL2** - This allows Docker to interact with and use resources in yoru WSL2 installation on Windows 11.

**Pulling and Running the Containe for the First Time.** - 
The script below starts the xhost to allow UI's to interact with Windows.  Next it starts up the container with a default path of "c:/Projects" and mounts that volume into the container (change /mnt/c/Projects to your own work path) to allow the container to interact with your local file system.   Finally it starts the QT Creator application in the container projecting it onto your windows desktop.   To use this script save it with the extention .sh and run from your WSL installation.

~~~
#! /bin/sh
xhost +
PULSE_SERVER_TCP_ENV="-e PULSE_SERVER=tcp:172.17.0.1:4713"
docker run -it --privileged -v /tmp/.X11-unix:/tmp/.X11-unix -v /home/`id -nu`:/home/`id -nu` -v /mnt/c/Projects:/projects -e DISPLAY=unix$DISPLAY $PULSE_SERVER_TCP_ENV  --name Ahsoka.QTCreator openpv/openview_qt_creator ./entrypoint.sh
~~~


**Running The Container the Second Time** - 
By default within the same session (no logout), the container can simply be restarted using the docker run command. However on subsequent reboots, you will need to start xhost again and then the container with the commands below.
~~~
#! /bin/bash
xhost +
docker start Ahsoka.QTCreator --attach --interactive
~~~
