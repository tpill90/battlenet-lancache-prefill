Write-Host -ForegroundColor Yellow "Stopping Lancache.."
sudo docker stop lancache;

Write-Host -ForegroundColor Yellow "Clearing Lancache data"
sudo rm -rf /mnt/md0/lancache/cache; 

sudo docker start lancache;
