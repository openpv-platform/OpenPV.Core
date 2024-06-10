FROM ubuntu:20.04 AS build

ENV DEBIAN_FRONTEND=noninteractive

SHELL ["/bin/bash", "-c"]

RUN apt update && apt install  openssh-server sudo -y

RUN apt-get install -y python3
RUN apt-get install -y unzip
COPY systemctl3.py /usr/bin/systemctl

RUN echo "root:$(mkpasswd -s </dev/null)" | chpasswd -e
RUN useradd -m openpv
RUN passwd -d openpv
RUN sed -i'' -e's/^#PermitRootLogin prohibit-password$/PermitRootLogin yes/' /etc/ssh/sshd_config \
        && sed -i'' -e's/^#PasswordAuthentication yes$/PasswordAuthentication yes/' /etc/ssh/sshd_config \
        && sed -i'' -e's/^#PermitEmptyPasswords no$/PermitEmptyPasswords yes/' /etc/ssh/sshd_config \
        && sed -i'' -e's/^UsePAM yes/UsePAM no/' /etc/ssh/sshd_config

ADD resize-helper.service /etc/systemd/system/resize-helper.service
ADD bsp-info /usr/bin
ADD bsp-version /usr/share/bsp-info/bsp-version
ADD bsp-description /usr/share/bsp-info/bsp-description

# Start SSH service
RUN service ssh start

# Expose docker port 22
EXPOSE 22

CMD ["/usr/sbin/sshd","-D"]
#ENTRYPOINT service ssh start && bash -l
