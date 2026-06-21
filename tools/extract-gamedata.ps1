<#
.SYNOPSIS
    Recover the Slay the Spire 2 Godot project (assets, scenes, localization,
    resources) from SlayTheSpire2.pck into a gitignored ./gamedata folder for
    local inspection.

.DESCRIPTION
    Runs GDRE Tools (gdsdecomp) in headless mode to perform a full project
    recovery of the base game .pck. The result is dropped under <repo>/gamedata,
    which is .gitignored so the (large, copyrighted) extracted tree is never
    committed.

    Re-run this whenever the game updates to refresh your reference copy. Note
    that C# game logic lives in sts2.dll, NOT the .pck -- decompile that
    separately (e.g. ilspycmd / Rider). The .pck gives you text, art, scenes,
    shaders and resources.

.PARAMETER Sts2Path
    Path to the Slay the Spire 2 install folder (containing SlayTheSpire2.pck).
    Auto-discovered from the Steam library registry if not given.

.PARAMETER OutputDir
    Where to recover the project. Default <repo>/gamedata.

.PARAMETER Gdre
    Path to the gdre_tools executable. Auto-discovered from PATH if not given.

.PARAMETER Force
    Overwrite an existing OutputDir instead of aborting.

.EXAMPLE
    ./extract-gamedata.ps1
    ./extract-gamedata.ps1 -Force
#>
param(
    [string]$Sts2Path = "",
    [string]$OutputDir = "",
    [string]$Gdre = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Repo root is the parent of this tools/ folder.
$repoRoot = Split-Path -Parent $PSScriptRoot

# --- Resolve the game install folder ---------------------------------------
function Resolve-Sts2Path {
    param([string]$Override)
    if ($Override) { return $Override }

    $candidates = @()
    try {
        $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
        if ($steam) { $candidates += (Join-Path $steam 'steamapps\common\Slay the Spire 2') }
    } catch {}
    $candidates += 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'

    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c 'SlayTheSpire2.pck')) { return $c }
    }
    throw "Could not find SlayTheSpire2.pck. Pass -Sts2Path explicitly."
}

# --- Resolve gdre_tools -----------------------------------------------------
function Resolve-Gdre {
    param([string]$Override)
    if ($Override) {
        if (-not (Test-Path $Override)) { throw "gdre_tools not found at: $Override" }
        return $Override
    }
    $cmd = Get-Command gdre_tools -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "gdre_tools not on PATH. Install it (scoop install gdsdecomp) or pass -Gdre."
    }
    # A scoop shim launches the real exe detached and swallows the exit code.
    # Follow the .shim to the real exe so we can wait on it reliably.
    $shim = [IO.Path]::ChangeExtension($cmd.Source, 'shim')
    if (Test-Path $shim) {
        $line = (Get-Content $shim | Where-Object { $_ -match '^\s*path\s*=' } | Select-Object -First 1)
        if ($line -and ($line -match '"(.+?)"')) { return $Matches[1] }
    }
    return $cmd.Source
}

$gameDir = Resolve-Sts2Path -Override $Sts2Path
$pck     = Join-Path $gameDir 'SlayTheSpire2.pck'
$gdreExe = Resolve-Gdre -Override $Gdre
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'gamedata' }

if (-not (Test-Path $pck)) { throw "Game .pck not found: $pck" }

Write-Host "Game dir  : $gameDir"
Write-Host "Source pck: $pck"
Write-Host "Output    : $OutputDir"
Write-Host "gdre_tools: $gdreExe"

# --- Handle existing output -------------------------------------------------
if (Test-Path $OutputDir) {
    if (-not $Force) {
        throw "Output dir already exists: $OutputDir`nRe-run with -Force to overwrite (e.g. after a game update)."
    }
    Write-Host "Removing existing output (-Force)..."
    Remove-Item -Recurse -Force $OutputDir
}

# --- Recover ----------------------------------------------------------------
# Full project recovery: decompiled scenes/resources + extracted assets & text.
Write-Host "Recovering project (this can take a minute)..."
# Quote the value side of each arg: Start-Process splits array elements on
# spaces, and the game path contains spaces ("Slay the Spire 2").
$proc = Start-Process -FilePath $gdreExe `
    -ArgumentList @('--headless', "--recover=`"$pck`"", "--output=`"$OutputDir`"") `
    -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    throw "gdre_tools exited with code $($proc.ExitCode)."
}

# GDRE can exit 0 even on a partial run; sanity-check the recovered tree.
if (-not (Test-Path (Join-Path $OutputDir 'localization'))) {
    throw "Recovery looks incomplete: no 'localization' folder under $OutputDir."
}

Write-Host ""
Write-Host "Done. Recovered game project at: $OutputDir"
Write-Host "  Text/localization : $OutputDir\localization"
Write-Host "  Assets            : images / scenes / audio folders"
Write-Host "  (C# game logic is in sts2.dll, not here -- decompile that separately.)"
