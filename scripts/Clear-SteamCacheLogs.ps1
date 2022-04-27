Write-Host -ForegroundColor Yellow "Clearing Lancache logs.."

sudo docker stop lancache | Out-Null;
sudo rm /mnt/nvme0n1/lancache/cache/logs/access.log -f; 
sudo docker restart lancache| Out-Null;