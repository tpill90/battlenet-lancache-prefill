Push-Location C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs\

scp tim@192.168.1.222:/mnt/nvme0n1/lancache/cache/logs/access.log access.log
get-content access.log -ReadCount 1000 | foreach { $_ -match "MISS" } | out-file filtered.log
Pop-Location