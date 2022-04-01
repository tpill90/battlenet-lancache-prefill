Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

# Windows publish
foreach($runtime in @("win-x64"))
{
    Write-Host "Publishing $runtime" -ForegroundColor Cyan
    dotnet publish .\BattleNetPrefill\BattleNetPrefill.csproj `
    -o publish/BattleNetBackup-$runtime `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:PublishTrimmed=true

    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/BattleNetBackup-$runtime | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    Compress-Archive -path publish/BattleNetBackup-$runtime publish/$runtime.zip
}

# Doing linux and osx separatly, they don't support ReadyToRun
foreach($runtime in @("linux-x64", "osx-x64"))
{
    Write-Host "Publishing $runtime" -ForegroundColor Cyan
    dotnet publish .\BattleNetPrefill\BattleNetPrefill.csproj `
    -o publish/BattleNetBackup-$runtime `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true

    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/BattleNetBackup-$runtime | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    Compress-Archive -path publish/BattleNetBackup-$runtime publish/$runtime.zip
}