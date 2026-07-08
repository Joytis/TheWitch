# Serve the Witch card design page and open it in the browser.
# Stop it with the "Card Designs: Stop" task (or Ctrl+C in this panel).
$ErrorActionPreference = "Stop"
$server = Join-Path $PSScriptRoot "..\Docs\card-data\server.js"
Start-Process "http://localhost:7820"
node $server
