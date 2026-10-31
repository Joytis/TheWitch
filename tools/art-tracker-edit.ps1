# Open the art tracker in EDIT MODE: served by the card-designs server (Docs/card-data/server.js), which
# answers /api/edit, so the page shows editable artist / stage / final / brief fields that save to the
# repo JSON. Reuses the server if it's already running (restart it after pulling server changes).
# Stop it with the "Card Designs: Stop" task (or Ctrl+C in this panel).
$ErrorActionPreference = "Stop"
$url = "http://localhost:7820/pages/art-tracker.html"
$server = Join-Path $PSScriptRoot "..\Docs\card-data\server.js"
$running = $false
try { $running = (Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:7820/api/edit" -TimeoutSec 2).StatusCode -eq 200 } catch {}
if ($running) {
    Write-Host "Server already running with edit mode -> $url"
    Start-Process $url
    return
}
if (Get-NetTCPConnection -LocalPort 7820 -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "Port 7820 is taken by an older card-designs server without edit mode. Stop it ('Card Designs: Stop') and run this again."
    exit 1
}
Start-Process $url
node $server
