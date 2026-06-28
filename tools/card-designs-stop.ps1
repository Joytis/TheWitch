# Stop the Wicken card design server (the node process listening on port 7820).
$port = 7820
$pids = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
$killed = $false
foreach ($procId in $pids) {
    $p = Get-Process -Id $procId -ErrorAction SilentlyContinue
    if ($p -and $p.ProcessName -eq "node") {
        Stop-Process -Id $procId -Force
        Write-Host "Stopped node server (PID $procId) on port $port."
        $killed = $true
    }
}
if (-not $killed) { Write-Host "No card-design server running on port $port." }
