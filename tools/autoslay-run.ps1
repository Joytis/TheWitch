<#
.SYNOPSIS
    One headless AutoSlay run of a character mod (Witch by default), skimmed for errors.

.DESCRIPTION
    Launches `launch.ps1 -Character <c> -Solo -AutoSlay -Headless` with a seed and a
    per-run AutoSlay log, waits for exit (run cap is 25 min), then copies the
    game's godot.log next to it and writes a JSON summary:

        <OutDir>/<N>-<seed>.autoslay.log   AutoSlay's own progress log (game-native -log-file)
        <OutDir>/<N>-<seed>.godot.log      full engine/mod log for the run
        <OutDir>/<N>-<seed>.summary.json   { seed, exitCode, minutes, floor, failure, errors[] }

    `errors[]` is every distinct [ERROR] line (count + first stack line) minus
    the known-noise filter below. `failure` is the "[AutoSlay] Run failed"
    message + stack when the bot bailed. The script's own exit code is 0 when
    the run completed AND no unfiltered errors were logged, 1 otherwise.

    Runs are serial by design: every instance rotates the same godot.log, so
    concurrent instances would clobber each other's engine log.

.EXAMPLE
    ./tools/autoslay-run.ps1 -OutDir $env:TEMP/autoslay -Index 3
    ./tools/autoslay-run.ps1 -OutDir out -Seed 8V3KQ2 -Build publish   # reproduce a seed after a fix
#>
param(
    [ValidateSet('Witch','Augur')]
    [string]$Character = 'Witch',
    [Parameter(Mandatory)][string]$OutDir,
    [int]$Index = 0,
    [string]$Seed = "",
    [ValidateSet('none','build','publish')]
    [string]$Build = 'none',
    [switch]$Windowed            # debugging aid: skip --headless
)

$ErrorActionPreference = 'Stop'
$launcher = Join-Path $PSScriptRoot 'launch.ps1'
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path

if (-not $Seed) {
    # Same alphabet the game uses for display seeds; 7 chars is plenty.
    $alphabet = 'ABCDEFGHIJKLMNPQRSTUVWXYZ123456789'.ToCharArray()
    $Seed = -join (1..7 | ForEach-Object { $alphabet[(Get-Random -Maximum $alphabet.Length)] })
}
$stem       = Join-Path $OutDir ("{0:D3}-{1}" -f $Index, $Seed)
$asLog      = "$stem.autoslay.log"
$godotCopy  = "$stem.godot.log"
$summary    = "$stem.summary.json"
$godotLog   = Join-Path $env:APPDATA 'SlayTheSpire2\logs\godot.log'

# Noise: engine teardown spam + lines that are not mod/game logic failures.
$noise = @(
    'RID allocations of type',
    'resources still in use at exit',
    'ObjectDB instances leaked',
    'FMOD',                                # audio device chatter under --headless
    'Sentry',
    'Parameter "t" is null',               # dummy renderer texture_2d_initialize under --headless
    'Parameter "texture" is null',         # dummy renderer texture_free at exit under --headless
    'Expected BoundObject to be a SpineSprite'  # NMerchantCharacter (BaseLib-patched) spine binding under the dummy renderer; not ours
    'but its creature node doesn''t exist'     # CubexConstruct.RepeaterBlastMove: dies to Brambles mid-attack, then TriggerAnim(AttackEnd) on the freed node; base-game log-not-throw
)

Write-Host "[autoslay-run] #$Index seed=$Seed -> $stem.*"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$launchArgs = @{ Solo = $true; AutoSlay = $true; Seed = $Seed; AutoSlayLog = $asLog; Build = $Build }
if (-not $Windowed) { $launchArgs.Headless = $true }
& $launcher @launchArgs
$exit = $LASTEXITCODE
$sw.Stop()

if (Test-Path $godotLog) { Copy-Item $godotLog $godotCopy -Force }
$lines = if (Test-Path $godotCopy) { Get-Content $godotCopy } else { @() }
$asLines = if (Test-Path $asLog) { Get-Content $asLog } else { @() }

# --- Skim -----------------------------------------------------------------
$errLines = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -notmatch '^\[ERROR\]|^ERROR:') { continue }
    $skip = $false
    foreach ($n in $noise) { if ($l -like "*$n*") { $skip = $true; break } }
    if ($skip) { continue }
    $msg = $l -replace '^\[ERROR\]\s*|^ERROR:\s*', ''
    # First "   at ..." frame after the message, for a stable dedupe key + pointer.
    $frame = ''
    for ($j = $i + 1; $j -lt [Math]::Min($i + 6, $lines.Count); $j++) {
        if ($lines[$j] -match '^\s+at ') { $frame = $lines[$j].Trim(); break }
    }
    $errLines += [pscustomobject]@{ message = $msg; frame = $frame }
}
$errors = $errLines | Group-Object { "$($_.message)|$($_.frame)" } | ForEach-Object {
    [pscustomobject]@{ count = $_.Count; message = $_.Group[0].message; frame = $_.Group[0].frame }
} | Sort-Object count -Descending

$failure = $null
# Empty AutoSlay log (game died before the bot started) — [string[]]@() casts to null in PS 5.1.
$failIdx = if ($asLines.Count -gt 0) { [array]::FindIndex([string[]]$asLines, [Predicate[string]]{ param($x) $x -match '\[AutoSlay\] Run failed' }) } else { -1 }
if ($failIdx -ge 0) {
    $failure = ($asLines[$failIdx..([Math]::Min($failIdx + 12, $asLines.Count - 1))] -join "`n")
}
# Last bot actions before the failure (watchdog chatter dropped) — the triage context.
$lastActions = @()
if ($failIdx -ge 0) {
    $lastActions = @($asLines[0..($failIdx - 1)] | Where-Object { $_ -notmatch 'Watchdog|MemProfile' } | Select-Object -Last 10)
}
$completed = [bool]($asLines | Where-Object { $_ -match 'Run completed successfully' })
$floor = ($asLines | Where-Object { $_ -match 'Entering \w+ room \(Act (\d+), Floor (\d+)\)' } |
          ForEach-Object { "A$($Matches[1])F$($Matches[2])" } | Select-Object -Last 1)
$watchdog = @($asLines | Where-Object { $_ -match 'Watchdog|no progress' }).Count

$result = [ordered]@{
    index      = $Index
    seed       = $Seed
    exitCode   = $exit
    completed  = $completed
    minutes    = [Math]::Round($sw.Elapsed.TotalMinutes, 1)
    lastFloor  = $floor
    watchdogHits = $watchdog
    failure    = $failure
    lastActions = @($lastActions)
    errors     = @($errors)
    files      = [ordered]@{ autoslay = $asLog; godot = $godotCopy }
}
$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 $summary

$ok = $completed -and $errors.Count -eq 0 -and $exit -eq 0
$color = if ($ok) { 'Green' } else { 'Yellow' }
Write-Host ("[autoslay-run] #{0} seed={1} exit={2} completed={3} floor={4} {5}min errors={6}" -f `
    $Index, $Seed, $exit, $completed, $floor, $result.minutes, $errors.Count) -ForegroundColor $color
foreach ($e in $errors) { Write-Host ("  {0,4}x  {1}" -f $e.count, ($e.message.Substring(0, [Math]::Min(200, $e.message.Length)))) -ForegroundColor Red }
if ($failure) { Write-Host "  FAILURE: $($failure.Split("`n")[0])" -ForegroundColor Red }
Write-Host "[autoslay-run] summary: $summary"
exit $(if ($ok) { 0 } else { 1 })
