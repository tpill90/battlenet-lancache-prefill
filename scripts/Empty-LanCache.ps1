Clear-Host;
docker container stop lancache;
docker rm lancache;
docker volume prune -f;

docker volume create lancache-volume;
docker run --restart unless-stopped --name lancache --detach `
    -v lancache-volume:/data/cache `
    -v lancache-volume:/data/logs `
    -p 80:80 `
    lancachenet/monolithic:latest

#Start-Sleep -Seconds 1;
Write-Host "Container reset" -ForegroundColor Yellow
#.\SteamcacheDockerLogs.ps1;