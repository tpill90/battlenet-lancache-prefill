Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

$runtimeIdentifiers = @("win-x64", "linux-x64", "osx-x64")

foreach($runtime in $runtimeIdentifiers)
{
    dotnet publish .\BattleNetPrefill\BattleNetPrefill.csproj `
    -o publish/$runtime `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true

    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/$runtime | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    Compress-Archive -path publish/$runtime publish/$runtime.zip
}