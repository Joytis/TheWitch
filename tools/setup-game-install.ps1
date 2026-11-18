<#
.SYNOPSIS
    Cache the currently installed Slay the Spire 2 as this repo's copy of one game branch:
    game-installs/<branch>/ (gitignored). Builds and tools select it with -Branch / -p:Sts2Branch.

.DESCRIPTION
    The mod is one source tree built against two game versions (public + beta), so both installs
    have to exist side by side. Steam only keeps one, hence the copies:

        1. Steam > Slay the Spire 2 > Properties > Betas: pick the branch, wait for the update.
        2. ./tools/setup-game-install.ps1 -Branch beta      (or public)
        3. Repeat for the other branch, then leave Steam on whichever you play.

    Re-run after the game patches that branch (it mirrors, so only changed files are copied).
    The script refuses when Steam's selected branch is not the one asked for, so a copy can never
    be filed under the wrong name.

    The copy's mods/ folder is left alone: it is not taken from the Steam install, and whatever the
    builds have deployed into the copy survives a refresh. Workshop subscriptions (BaseLib) still
    load, since the copy talks to the same Steam client.

.PARAMETER Branch
    public | beta - which branch the Steam install is on right now.

.PARAMETER Sts2Path
    The Steam install to copy from. Auto-discovered when omitted.

.EXAMPLE
    ./tools/setup-game-install.ps1 -Branch beta
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('public','beta')]
    [string]$Branch,
    [string]$Sts2Path = ""
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'characters.ps1')

if (-not $Sts2Path) { $Sts2Path = Get-SteamSts2Path }
if (-not (Test-Path (Join-Path $Sts2Path 'SlayTheSpire2.exe'))) { throw "No SlayTheSpire2.exe in '$Sts2Path'." }

$steamBranch = Get-SteamSts2Branch $Sts2Path
if ($steamBranch -and $steamBranch -ne $Branch) {
    throw "Steam has Slay the Spire 2 on its '$steamBranch' branch, not '$Branch'. Switch it under Properties > Betas (and let it finish updating) first."
}
if (-not $steamBranch) { Write-Warning "Could not read Steam's selected branch for '$Sts2Path'; trusting -Branch $Branch." }
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'Slay the Spire 2 is running; close it first.' }

$dest = Join-Path $repoRoot "game-installs/$Branch"
$info = Join-Path $Sts2Path 'release_info.json'
$version = if (Test-Path $info) { (Get-Content $info -Raw | ConvertFrom-Json).version } else { 'unknown' }
Write-Host "==> Caching game $version ($Branch) from $Sts2Path" -ForegroundColor Cyan
Write-Host "    -> $dest"

# /MIR mirrors; /XD mods keeps the Steam install's mods out AND protects the copy's own mods/.
& robocopy $Sts2Path $dest /MIR /XD (Join-Path $Sts2Path 'mods') (Join-Path $dest 'mods') /NFL /NDL /NJH /NP /R:2 /W:2 | Out-Host
if ($LASTEXITCODE -ge 8) { throw "robocopy failed (exit $LASTEXITCODE)." }   # 0-7 are success codes
New-Item -ItemType Directory -Force (Join-Path $dest 'mods') | Out-Null

$copied = (Get-Content (Join-Path $dest 'release_info.json') -Raw | ConvertFrom-Json).version
Write-Host "==> game-installs/$Branch is game $copied" -ForegroundColor Green
exit 0
