<#
.SYNOPSIS
    Captures the Workshop marketing screenshots in-game: one game launch per shot, staged and
    saved by the BirdDebug harness (BirdDebug/BirdDebugCode/MarketingShots.cs, -bird-shot=<name>).

.DESCRIPTION
    Reads <workspace>/marketing/screenshots.json:
        { "size": "1920x1080",
          "shots": [ { "shot": "familiars", "file": "shot-familiars.png", "crop": [x, y, w, h] }, ... ] }
    "shot" is a MarketingShots name (charselect | familiars | restsite | mp-relics); "crop" (optional)
    is in window pixels at "size". Output lands next to the json, ready to list in marketing.json.

    Each shot opens a game window (rendering needs one) and closes it when done; don't touch the
    mouse while a window is up. mp-relics launches a -fastmp host + client pair on localhost; the
    host shoots, the client is closed afterwards. Needs the character mod + BirdDebug deployed
    (-Build build|publish does it; publish if art or scenes changed).

.PARAMETER Shots
    Only these shot names (default: every shot in screenshots.json).

.PARAMETER NoCrop
    Ignore the crop rects and write <file>.full.png instead (for tuning a crop).

.EXAMPLE
    ./tools/capture-marketing.ps1                         # every shot, Witch
    ./tools/capture-marketing.ps1 -Shots restsite -NoCrop # full frame, to pick a crop rect
#>
param(
    [ValidateSet('Witch','Augur')]
    [string]$Character = 'Witch',
    [string[]]$Shots = @(),
    [switch]$NoCrop,
    [ValidateSet('none','build','publish')]
    [string]$Build = 'none',
    [int]$TimeoutSec = 240,
    [string]$Sts2Path = ""
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'characters.ps1')
$Char = Get-BirdCharacter $Character
$marketingDir = Join-Path $repoRoot (Join-Path $Char.Workshop 'marketing')
$configPath = Join-Path $marketingDir 'screenshots.json'
if (-not (Test-Path $configPath)) { throw "Shot list not found: $configPath" }
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$size = if ($config.size) { $config.size } else { '1920x1080' }

if ($Build -ne 'none') {
    foreach ($proj in @($Char.Csproj, $BirdDebugCsproj)) {
        $verb = if ($proj -eq $BirdDebugCsproj) { 'build' } else { $Build }
        Write-Host "[$verb] $proj"
        & dotnet $verb (Join-Path $repoRoot $proj)
        if ($LASTEXITCODE -ne 0) { throw "dotnet $verb $proj failed (exit $LASTEXITCODE)." }
    }
}

# --- game install (same lookup as launch.ps1) ------------------------------------------------
if (-not $Sts2Path) {
    $candidates = @()
    try {
        $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
        if ($steam) { $candidates += (Join-Path $steam 'steamapps\common\Slay the Spire 2') }
    } catch {}
    $candidates += 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
    $Sts2Path = $candidates | Where-Object { Test-Path (Join-Path $_ 'SlayTheSpire2.exe') } | Select-Object -First 1
    if (-not $Sts2Path) { throw "Could not find SlayTheSpire2.exe. Pass -Sts2Path." }
}
$exe = Join-Path $Sts2Path 'SlayTheSpire2.exe'
$appId = Join-Path $Sts2Path 'steam_appid.txt'
if (-not (Test-Path $appId)) { Set-Content -Path $appId -Value '2868840' -NoNewline -Encoding ascii }
$env:STS2_DEV_SKIP = '1'   # skip the intro logo (inherited by the game process)

function Start-Game([string[]]$gameArgs) {
    Start-Process -FilePath $exe -WorkingDirectory $Sts2Path -ArgumentList $gameArgs -PassThru
}

$selected = @($config.shots | Where-Object { $Shots.Count -eq 0 -or $Shots -contains $_.shot })
if ($selected.Count -eq 0) { throw "No shots selected (have: $(($config.shots | ForEach-Object shot) -join ', '))." }

$failed = @()
foreach ($s in $selected) {
    $file = $s.file
    if ($NoCrop) { $file = [IO.Path]::ChangeExtension($file, '.full.png') }
    $out = Join-Path $marketingDir $file
    if (Test-Path $out) { Remove-Item $out -Force }

    $gameArgs = @("-bird-character=$($Char.Key)", "-bird-shot=$($s.shot)", "-bird-shot-out=`"$out`"", "-bird-shot-size=$size")
    if ($s.crop -and -not $NoCrop) { $gameArgs += "-bird-shot-crop=$($s.crop -join ',')" }

    Write-Host "==> $($s.shot) -> $file" -ForegroundColor Cyan
    $client = $null
    if ($s.shot -eq 'mp-relics') {
        $host_ = Start-Game ($gameArgs + @('-fastmp', 'host_standard'))
        Start-Sleep -Seconds 4
        $client = Start-Game @("-bird-character=$($Char.Key)", "-bird-shot=$($s.shot)", "-bird-shot-size=$size", '-fastmp', 'join', '-clientId', '1000')
        $proc = $host_
    } else {
        $proc = Start-Game $gameArgs
    }

    if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
        Write-Host "[warn] timed out after ${TimeoutSec}s; closing the game" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    if ($client -and -not $client.HasExited) { Stop-Process -Id $client.Id -Force -ErrorAction SilentlyContinue }

    if (Test-Path $out) {
        Write-Host "    wrote $out" -ForegroundColor Green
    } else {
        Write-Host "    FAILED (no image; see %APPDATA%\SlayTheSpire2\logs\godot.log, tag [bird-shot])" -ForegroundColor Red
        $failed += $s.shot
    }
}
if ($failed.Count) { throw "Shots failed: $($failed -join ', ')" }
