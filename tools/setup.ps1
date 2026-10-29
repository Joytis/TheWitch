<#
.SYNOPSIS
    One-shot local setup for the character mod projects (fresh clone or refresh). Idempotent.

.DESCRIPTION
    Requires gamedata/ (the local decompile) at the repo root; see README "Setup".
    Per character project:
      1. gamedata junctions (tools/setup-gamedata-links.ps1)
      2. addons/mega_text copied from gamedata/addons/ (base-game scenes need it in the editor)
    Witch only:
      3. PROTOTYPE Live2D: GDCubism + Cubism SDK + sample model (tools/live2d/setup-live2d.ps1).
         Prompts once to accept Live2D's SDK license. A failure or a declined license only skips
         this step; -SkipLive2D skips it outright.

.EXAMPLE
    ./tools/setup.ps1
    ./tools/setup.ps1 -Character Witch -AcceptLive2DLicense
    ./tools/setup.ps1 -SkipLive2D
#>
param(
    [ValidateSet('Witch', 'Augur', 'All')]
    [string]$Character = 'All',
    [switch]$SkipLive2D,
    [switch]$AcceptLive2DLicense
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'characters.ps1')

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

$gamedata = Join-Path $repoRoot 'gamedata'
if (-not (Test-Path $gamedata)) {
    throw "gamedata/ not found at $repoRoot. Put the decompiled game source there first (README 'Setup')."
}
$targets = if ($Character -eq 'All') { @($BirdCharacters.Keys) } else { @($Character) }

Write-Step "gamedata junctions ($($targets -join ', '))"
& (Join-Path $PSScriptRoot 'setup-gamedata-links.ps1') -Character $Character

$megaText = Join-Path $gamedata 'addons/mega_text'
foreach ($key in $targets) {
    $dst = Join-Path $repoRoot (Join-Path $BirdCharacters[$key].ProjectDir 'addons/mega_text')
    if (Test-Path $dst) {
        Write-Host "ok:      $key addons/mega_text"
    } elseif (Test-Path $megaText) {
        New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
        Copy-Item $megaText $dst -Recurse
        Write-Host "created: $key addons/mega_text"
    } else {
        Write-Warning "gamedata/addons/mega_text missing; $key editor scenes using MegaText won't resolve."
    }
}

$live2dResult = 'skipped (not the Witch)'
if ($targets -contains 'Witch') {
    if ($SkipLive2D) {
        $live2dResult = 'skipped (-SkipLive2D)'
    } else {
        Write-Step "Witch Live2D prototype"
        try {
            & (Join-Path $PSScriptRoot 'live2d/setup-live2d.ps1') -AcceptLive2DLicense:$AcceptLive2DLicense
            $live2dResult = 'staged (publish TheWitch; the Witch then renders as Live2D)'
        } catch {
            Write-Warning "Live2D setup skipped: $($_.Exception.Message)"
            $live2dResult = "FAILED/skipped: $($_.Exception.Message)"
        }
    }
}

Write-Step "Done."
Write-Host "  junctions + mega_text: $($targets -join ', ')"
Write-Host "  Live2D: $live2dResult"
Write-Host "  next: dotnet publish <Character>/<Character>.csproj (or the Run / Debug tasks with Publish)"
