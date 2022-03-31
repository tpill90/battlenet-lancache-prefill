Write-Host -ForegroundColor Yellow "Downloading latest logs"
scp tim@192.168.1.222:/mnt/nvme0n1/lancache/cache/logs/access.log access.log

$allurls = (Get-Content -tail 10 access.log) `
    | Select-String -pattern "(/tpr/\w*/data/[a-z0-9]{2}/[a-z0-9]{2}/[a-z0-9]+)" -AllMatches `
    | Foreach-Object {$_.Matches.groups[0].value} `
    | Get-Unique
foreach($uri in $allurls)
{
    $downloadUrl = "http://level3.blizzard.com$uri"
    Write-Host $downloadUrl -ForegroundColor Yellow;
    &"C:\Users\Tim\Dropbox\Apps\Utility\curl-7.82.0-win64-mingw\bin\curl.exe" $downloadUrl -o nul
}
