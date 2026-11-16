<#
.SYNOPSIS
    Frame-time sanity check of combat Spine bodies: base-game characters vs mod characters.

.DESCRIPTION
    Launches the game once per character per run (interleaved, so drift hits both sides) with the
    BirdDebug perf probe (-bird-perf, BirdDebug/BirdDebugCode/PerfProbe.cs): fixed run + encounter,
    vsync off and the frame rate uncapped, then three phases of -Seconds each:
      baseline   normal combat
      frozen     the player's SpineSprite paused (baseline - frozen = the body's Spine update cost)
      copies     -Copies extra instances of the player's skeleton playing idle_loop
      update     those copies on manual update, each update_skeleton(1/60) timed directly: the real
                 per-instance Spine CPU cost at 60 fps (physics constraints step at most once per
                 1/60 s, so the uncapped frame-time phases under-count them)
    and prints a comparison table (means over runs). Needs the character mod(s) + BirdDebug deployed
    (-Build build|publish does it). Each launch opens a 1920x1080 window; leave it alone while it runs.
    Any character in the game works for -Characters (e.g. Ironclad, Silent, Witch); mod characters need
    their mod installed.

.EXAMPLE
    ./tools/perf-spine.ps1                                   # Ironclad vs Witch, 2 runs each, 10 s phases
    ./tools/perf-spine.ps1 -Characters Ironclad,Silent,Witch -Runs 3 -Copies 16
#>
param(
    [string[]]$Characters = @('Ironclad', 'Witch'),
    [int]$Runs = 2,
    [double]$Seconds = 10,
    [int]$Copies = 8,
    [string]$OutDir = '',
    [ValidateSet('none', 'build', 'publish')]
    [string]$Build = 'none',
    [string]$Sts2Path = '',
    [int]$TimeoutSec = 240
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'characters.ps1')

if ($Build -ne 'none') {
    $projects = @($Characters | Where-Object { $BirdCharacters.Contains($_) } | ForEach-Object { $BirdCharacters[$_].Csproj }) + $BirdDebugCsproj
    foreach ($proj in $projects) {
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

if (-not $OutDir) { $OutDir = Join-Path ([IO.Path]::GetTempPath()) ("perf-spine-" + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$inv = [Globalization.CultureInfo]::InvariantCulture

$results = @{}
foreach ($c in $Characters) { $results[$c] = @() }
for ($r = 1; $r -le $Runs; $r++) {
    foreach ($c in $Characters) {
        $out = Join-Path $OutDir "$c-$r.json"
        Write-Host "==> run $r/$Runs : $c" -ForegroundColor Cyan
        $gameArgs = @("-bird-character=$c", "-bird-perf=$($Seconds.ToString($inv))", "-bird-perf-copies=$Copies", "-bird-perf-out=`"$out`"")
        $proc = Start-Process -FilePath $exe -WorkingDirectory $Sts2Path -ArgumentList $gameArgs -PassThru
        if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            throw "$c run $r timed out after ${TimeoutSec}s (see %APPDATA%\SlayTheSpire2\logs\godot.log, tag [bird-perf])."
        }
        if (-not (Test-Path $out)) { throw "$c run $r wrote no result (exit $($proc.ExitCode); see godot.log, tag [bird-perf])." }
        $results[$c] += Get-Content $out -Raw | ConvertFrom-Json
    }
}

function Avg($values) { ($values | Measure-Object -Average).Average }
function F([double]$v, [string]$fmt = 'F2') { $v.ToString($fmt, $inv) }

Write-Host ""
$first = $results[$Characters[0]][0]
Write-Host "GPU: $($first.gpu)   window: $($first.window)   $Runs run(s) x $(F $Seconds 'F0') s per phase, $Copies copies" -ForegroundColor Green
Write-Host ""
Write-Host "Skeletons:"
foreach ($c in $Characters) {
    $s = $results[$c][0].skeleton
    Write-Host ("  {0,-10} bones {1,4}  slots {2,4}  physics {3,4}  ik {4,3}  transform {5,3}  animations {6,3}" -f $c, $s.bones, $s.slots, $s.physicsConstraints, $s.ikConstraints, $s.transformConstraints, $s.animations)
}
Write-Host ""
Write-Host ("{0,-10} {1,-12} {2,8} {3,10} {4,8} {5,8} {6,10} {7,8} {8,7}" -f 'character', 'phase', 'fps', 'frame ms', 'p95', 'p99', 'render cpu', 'gpu ms', 'draws')
foreach ($c in $Characters) {
    $runsFor = $results[$c]
    for ($p = 0; $p -lt $runsFor[0].phases.Count; $p++) {
        $ph = @($runsFor | ForEach-Object { $_.phases[$p] })
        Write-Host ("{0,-10} {1,-12} {2,8} {3,10} {4,8} {5,8} {6,10} {7,8} {8,7}" -f $c, $ph[0].Name,
            (F (Avg ($ph | ForEach-Object Fps)) 'F0'), (F (Avg ($ph | ForEach-Object FrameMsMean))),
            (F (Avg ($ph | ForEach-Object FrameMsP95))), (F (Avg ($ph | ForEach-Object FrameMsP99))),
            (F (Avg ($ph | ForEach-Object RenderCpuMsMean))), (F (Avg ($ph | ForEach-Object GpuMsMean))),
            (F (Avg ($ph | ForEach-Object DrawCallsMean)) 'F0'))
    }
}
Write-Host ""
Write-Host "Uncapped frame-time deltas (physics under-counted: it steps at most once per 1/60 s):"
foreach ($c in $Characters) {
    $runsFor = $results[$c]
    Write-Host ("  {0,-10} body (baseline - frozen) {1} ms   +1 copy: frame {2} ms, gpu {3} ms, {4} draw calls" -f $c,
        (F (Avg ($runsFor | ForEach-Object bodyFrameMs)) 'F3'), (F (Avg ($runsFor | ForEach-Object perCopyFrameMs)) 'F3'),
        (F (Avg ($runsFor | ForEach-Object perCopyGpuMs)) 'F3'), (F (Avg ($runsFor | ForEach-Object perCopyDrawCalls)) 'F0'))
}
Write-Host ""
Write-Host "Spine CPU per instance per 60 fps frame, update_skeleton(1/60) timed directly (budget 16667 us):" -ForegroundColor Green
foreach ($c in $Characters) {
    $u = @($results[$c] | ForEach-Object update)
    Write-Host ("  {0,-10} mean {1} us   p50 {2}   p95 {3}   p99 {4}   max {5}" -f $c,
        (F (Avg ($u | ForEach-Object UsMean)) 'F0'), (F (Avg ($u | ForEach-Object UsP50)) 'F0'), (F (Avg ($u | ForEach-Object UsP95)) 'F0'),
        (F (Avg ($u | ForEach-Object UsP99)) 'F0'), (F (($u | ForEach-Object UsMax | Measure-Object -Maximum).Maximum) 'F0'))
}
Write-Host ""
Write-Host "Where one update goes (mean us; 'no physics' = all physics constraints at mix 0, about what a baked rig costs):"
foreach ($c in $Characters) {
    $rows = @($results[$c][0].updateBreakdown)
    $line = for ($i = 0; $i -lt $rows.Count; $i++) {
        $name = $rows[$i].Name
        $vals = @($results[$c] | ForEach-Object { $_.updateBreakdown[$i].UsMean })
        "{0} {1}" -f $name, (F (Avg $vals) 'F0')
    }
    Write-Host ("  {0,-10} {1}" -f $c, ($line -join '  |  '))
}
Write-Host ""
Write-Host "Raw JSON: $OutDir"
