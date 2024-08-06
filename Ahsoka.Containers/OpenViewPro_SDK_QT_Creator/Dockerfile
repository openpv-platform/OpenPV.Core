# syntax=docker/dockerfile:1

FROM openpv/openviewpro_sdk_3_0

ENV DEBIAN_FRONTEND=noninteractive

SHELL ["/bin/bash", "-c"]

RUN \
    apt-get update --fix-missing && \
    apt-get install -y --no-install-recommends \
        build-essential \
        qtcreator \
        qtbase5-dev \
		qtchooser \
		qt5-qmake \
		qtbase5-dev-tools \
		libqt5charts5-dev \
        qt5-doc \
        qtdeclarative5-dev \
        qml-module-qtquick-controls2 \
        qtbase5-examples \
        qtbase5-doc-html && \
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/*;



ADD qt_kit_pro.config /root/.config

ADD entrypoint_pro.sh /home/user/entrypoint.sh
RUN chmod 777 /home/user/entrypoint.sh

WORKDIR /home/user
