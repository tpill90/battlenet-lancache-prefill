Write-Host -ForegroundColor Yellow "Stopping Lancache.."
sudo docker stop lancache;
Write-Host -ForegroundColor Yellow "Clearing logs.."
sudo rm /mnt/nvme0n1/lancache/cache/logs/access.log -f; 
sudo docker restart lancache;