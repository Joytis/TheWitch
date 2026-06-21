<#
.SYNOPSIS
    Run a headless Godot in --import mode to generate the .import files (and the
    .godot/imported cache) for every asset in the project -- e.g. new .png card
    portraits dropped under TheWicken/images.

.DESCRIPTION
    Godot does not track raw assets directly; it tracks the generated <asset>.import
    sidecar files. A freshly added .png has no .import until the project is imported,
    so it won't resolve at runtime / won't pack into the .pck. Opening the editor
    triggers this, but in a build/CI flow you want it headless.

    This runs:  godot --headless --path <repo> --import
    which imports all resources, writes the missing .import files, then quits.

    Godot version must be 4.5.x (same constraint as export-pack -- a newer Godot's
    output is rejected by the game).

.PARAMETER Godot
    Path to the Godot 4.5.x mono executable. Auto-discovered from the GodotPath
    value in Directory.Build.props if not given.

.EXAMPLE
    ./import-assets.ps1
    ./import-assets.ps1 -Godot "C:/megadot/MegaDot_v4.5.1-stable_mono_win64.exe"
#>
param(
    [string]$Godot = ""
)

$ErrorActionPreference = "Stop"

# Repo root is the parent of this tools/ folder.
$repoRoot = Split-Path -Parent $PSScriptRoot

# --- Resolve the Godot executable ------------------------------------------
function Resolve-Godot {
    param([string]$Override)
    if ($Override) {
        if (-not (Test-Path $Override)) { throw "Godot not found at: $Override" }
        return $Override
    }
    # Reuse the same GodotPath the build uses for export-pack.
    $props = Join-Path $repoRoot 'Directory.Build.props'
    if (Test-Path $props) {
        $text = Get-Content $props -Raw
        if ($text -match '<GodotPath>\s*(.+?)\s*</GodotPath>') {
            $path = $Matches[1].Trim()
            if (Test-Path $path) { return $path }
            throw "GodotPath in Directory.Build.props points at a missing file: $path"
        }
    }
    throw "Could not resolve Godot. Set <GodotPath> in Directory.Build.props or pass -Godot."
}

$godotExe = Resolve-Godot -Override $Godot
$projFile = Join-Path $repoRoot 'project.godot'
if (-not (Test-Path $projFile)) { throw "project.godot not found at repo root: $repoRoot" }

Write-Host "Godot   : $godotExe"
Write-Host "Project : $repoRoot"
Write-Host "Importing assets (headless)..."

# --headless: no window. --import: import all resources then quit.
$proc = Start-Process -FilePath $godotExe `
    -ArgumentList @('--headless', '--path', "`"$repoRoot`"", '--import') `
    -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    throw "Godot import exited with code $($proc.ExitCode)."
}

Write-Host ""
Write-Host "Done. .import files under TheWicken/images:"
Get-ChildItem -Path (Join-Path $repoRoot 'TheWicken/images') -Recurse -Filter '*.import' |
    ForEach-Object { Write-Host "  $($_.FullName.Substring($repoRoot.Length + 1))" }
