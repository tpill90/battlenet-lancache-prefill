sudo systemctl disable systemd-resolved
sudo systemctl stop systemd-resolved

docker kill $(docker ps -q)
docker rm $(docker ps -a -q)
docker run --restart unless-stopped --name lancache --detach     -v lancache-volume:/data/cache     -v lancache-volume:/data/logs     -p 80:80     -e CACHE_SLICE_SIZE=8m     lancachenet/monolithic:latest
docker run --restart unless-stopped         --name lancache-dns         --detach -p 53:53/udp         -e USE_GENERIC_CACHE=true         -e DISABLE_WSUS=true        -e LANCACHE_IP="192.168.110.128"     lancachenet/lancache-dns:latest
docker run --restart unless-stopped         --name sniproxy         --detach         -p 443:443        lancachenet/sniproxy:latest