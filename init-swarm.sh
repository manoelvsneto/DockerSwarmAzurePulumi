#!/bin/bash
apt-get update
apt-get install -y docker.io
systemctl start docker
docker swarm init --advertise-addr $(hostname -I | awk '{print $1}')
