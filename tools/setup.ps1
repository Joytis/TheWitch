<#
.SYNOPSIS
    One-shot local setup for the character mod projects (fresh clone or refresh). Idempotent.

.DESCRIPTION
    Requires gamedata/ (the local decompile) at the repo root; see README "Setup".
    Per character project:
      1. gamedata junctions (tools/setup-gamedata-links.ps1)
      2. addons/mega_text copied from gamedata/addons/ (base-game scenes need it in the editor)

.EXAMPLE
    ./tools/setup.ps1
    ./tools/setup.ps1 -Character Witch
#>
param(
    [ValidateSet('Witch', 'Augur', 'All')]
    [string]$Character = 'All'
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

Write-Step "Done."
Write-Host "  junctions + mega_text: $($targets -join ', ')"
Write-Host "  next: dotnet publish <Character>/<Character>.csproj (or the Run / Debug tasks with Publish)"
