FROM ubuntu:22.04 as build

ENV DEBIAN_FRONTEND=noninteractive

SHELL ["/bin/bash", "-c"]

# Dependencies sourced from https://wiki.st.com/stm32mpu-ecosystem-v3/wiki/Install_the_SDK
RUN apt-get update --fix-missing
RUN apt-get -y install gawk wget git-core 

# Optional Dependecies we don't need but keep here.
#RUN apt-get -y install gawk wget git-core diffstat unzip texinfo gcc-multilib build-essential chrpath socat cpio python3 python3-pip python3-pexpect xz-utils debianutils iputils-ping python3-git python3-jinja2 libegl1-mesa libsdl1.2-dev pylint3 pylint xterm
#RUN apt-get -y install make xsltproc docbook-utils fop dblatex xmlto
#RUN apt-get -y install libmpc-dev libgmp-dev
#RUN apt-get -y install libncurses5 libncurses5-dev libncursesw5-dev libssl-dev linux-headers-generic u-boot-tools device-tree-compiler bison flex g++ libyaml-dev libmpc-dev libgmp-dev
#RUN apt-get -y install coreutils bsdmainutils sed curl bc lrzsz corkscrew cvs subversion mercurial nfs-common nfs-kernel-server libarchive-zip-perl dos2unix texi2html diffstat libxml2-utils

# Support for Proto and ZMQ for C++ Ahsoka.a
RUN apt update --fix-missing
RUN apt-get -y install libzmq3-dev 

# Add XZ required by Toolchain  
RUN apt-get -y install xz-utils

# Needed for remote access
RUN apt-get -y install cmake ninja-build gdb rsync openssh-server --fix-missing

# Support for Proto and ZMQ for C++ Ahsoka.a
RUN apt install -y autoconf automake libtool g++ 
RUN apt-get -y install libzmq3-dev
 
# Install the Toolchain
WORKDIR /root
COPY arago-2023.10-toolchain-2023.10.sh  /root/arago-2023.10-toolchain-2023.10.sh
RUN mkdir -p /root/sdk
RUN chmod +x  /root/arago-2023.10-toolchain-2023.10.sh
RUN ./arago-2023.10-toolchain-2023.10.sh -y -d /root/sdk 
RUN rm -f  /root/sdk/arago-2023.10-toolchain-2023.10.sh

# Compile ProtoBuf Tools
RUN git clone --recursive https://github.com/protocolbuffers/protobuf.git
WORKDIR /root/protobuf
RUN git checkout v3.19.4 && git checkout -b release/v3.19.4
RUN ./autogen.sh
RUN ./configure
RUN make -j 8
RUN make install
RUN ldconfig

#Remove Proto Code
WORKDIR /root
RUN rm -rf /root/protobuf

# Setup Environment - Disabled
#RUN echo 'source /root/sdk/environment-setup-cortexa7t2hf-neon-vfpv4-ostl-linux-gnueabi' > /etc/profile.d/sdk.sh
#ADD bash.sh /etc/bash.bashrc

# Set up sshd
RUN echo 'PermitRootLogin yes' > /etc/ssh/sshd_config.d/permitroot.conf
RUN echo 'PermitEmptyPasswords yes' >> /etc/ssh/sshd_config.d/permitroot.conf
RUN passwd -d root
#EXPOSE 22

ENTRYPOINT service ssh start && bash -l

FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

COPY --from=build /root/sdk /root/sdk
COPY --from=build /usr/local/bin/protoc /usr/local/bin/protoc
COPY --from=build /usr/local/lib/ /usr/local/lib/
COPY --from=build /usr/local/include/google /usr/local/include/google

RUN \
    apt-get update --fix-missing && \
    apt-get install -y --no-install-recommends \
        gawk \
        wget \
        git-core \
        libzmq3-dev \
        cmake \
        ninja-build \
        gdb \
        rsync \
        openssh-server \
        autoconf \
        automake \
        libtool \
        g++ \
        libzmq3-dev \
        make \
        ca-certificates && \
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/*;
