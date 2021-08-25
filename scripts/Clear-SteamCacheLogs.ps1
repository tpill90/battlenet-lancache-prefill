Write-Host -ForegroundColor Yellow "Clearing lancache logs"
sudo docker stop lancache;
sudo rm /mnt/md0/lancache/cache/logs/access.log -f; 
sudo docker restart lancache;