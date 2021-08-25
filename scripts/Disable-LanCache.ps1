#Switching over to default automatic DNS resolution, won't be able to pull docker images if docker isnt running
$ethernetInterface = @(Get-NetAdapter) | Where-Object { $_.MediaType -eq 802.3 }
$ethernetInterface | Set-DnsClientServerAddress -ResetServerAddresses
Write-Host "Setting DNS resolution to automatic for adapters : " -NoNewline
Write-Host "$($ethernetInterface.Name)" -ForegroundColor Cyan

ipconfig /flushdns | Out-Null

Write-Host -ForegroundColor Yellow "Successfully disabled LanCache!"