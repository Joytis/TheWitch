<#
.SYNOPSIS
    Bundle the built mod into a Steam Workshop workspace and (optionally) upload it
    via MegaCrit's ModUploader.

.DESCRIPTION
    Produces an upload-ready workspace under <repo>/workshop and stages the
    deployable mod files into workshop/content. By default it first runs
    `dotnet publish` so the .pck is freshly exported, then copies the build
    outputs (TheWicken.json/.dll/.pck) from the game's mods folder into the
    workspace.

    Workspace layout consumed by ModUploader:
        workshop.json   metadata (title, description, visibility, tags, ...)   [versioned]
        image.png       Steam Workshop thumbnail                               [versioned]
        mod_id.txt      published item id, written after first upload          [versioned]
        content/        the mod files that actually get uploaded               [gitignored]

    workshop.json and image.png are hand-maintained and never clobbered; this
    script only stages content/ and (optionally) patches named metadata fields.

    Staging is the default. Uploading is opt-in (-Upload) because publishing to
    the Workshop is outward-facing and not trivially reversible.

.PARAMETER SkipPublish
    Skip `dotnet publish`; reuse whatever is already in the mods folder.
    The script warns if the staged .pck looks older than the source tree.

.PARAMETER Upload
    After staging, invoke `ModUploader upload`. Requires Steam to be running and
    logged in. Without this flag the script only stages and prints the command.

.PARAMETER ChangeNote
    Text written to workshop.json "changeNote" before uploading.

.PARAMETER Visibility
    Override workshop.json "visibility" (private | friends | friendsonly | public).

.PARAMETER IncludePdb
    Also copy TheWicken.pdb into content/ (debug symbols). Off by default.

.PARAMETER Workspace
    Workspace directory. Defaults to <repo>/workshop.

.PARAMETER ModsPath
    The game's mods/ folder. Auto-resolved from the build (msbuild ModsPath) if omitted.

.PARAMETER Uploader
    Path to ModUploader.exe. Defaults to tools/ModUploader-win-x64/ModUploader.exe.

.EXAMPLE
    ./bundle-workshop.ps1
    # publish + stage into workshop/content, print the upload command

.EXAMPLE
    ./bundle-workshop.ps1 -SkipPublish
    # stage current mods/ outputs without rebuilding

.EXAMPLE
    ./bundle-workshop.ps1 -Upload -ChangeNote "Phase 3 cards + potion brewing"
    # publish, stage, then upload to the Workshop
#>
[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$Upload,
    [string]$ChangeNote = "",
    [ValidateSet("", "private", "friends", "friendsonly", "public")]
    [string]$Visibility = "",
    [switch]$IncludePdb,
    [string]$Workspace = "",
    [string]$ModsPath = "",
    [string]$Uploader = ""
)

$ErrorActionPreference = "Stop"

# Repo root is the parent of this tools/ folder.
$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj   = Join-Path $repoRoot 'TheWicken.csproj'
$manifest = Join-Path $repoRoot 'TheWicken.json'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn2($msg) { Write-Host "[warn] $msg" -ForegroundColor Yellow }

# --- Read the mod manifest -------------------------------------------------
if (-not (Test-Path $manifest)) { throw "Mod manifest not found: $manifest" }
$mod = Get-Content $manifest -Raw | ConvertFrom-Json
$modId = $mod.id
if (-not $modId) { throw "Manifest has no 'id': $manifest" }

# --- Resolve the ModUploader executable ------------------------------------
if (-not $Uploader) {
    $Uploader = Join-Path $PSScriptRoot 'ModUploader-win-x64/ModUploader.exe'
}
if (-not (Test-Path $Uploader)) { throw "ModUploader.exe not found: $Uploader" }
$uploaderDir = Split-Path -Parent $Uploader

# --- Resolve the workspace -------------------------------------------------
if (-not $Workspace) { $Workspace = Join-Path $repoRoot 'workshop' }

# --- Step 1: publish (default) ---------------------------------------------
if ($SkipPublish) {
    Write-Step "Skipping publish (-SkipPublish); reusing existing mods/ outputs."
} else {
    Write-Step "Publishing ($csproj) -- this exports the .pck via headless Godot..."
    & dotnet publish $csproj
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }
}

# --- Resolve the mods source folder ----------------------------------------
if (-not $ModsPath) {
    Write-Step "Resolving mods path from build..."
    $ModsPath = (& dotnet msbuild $csproj -getProperty:ModsPath -nologo 2>$null | Select-Object -Last 1)
    if ($ModsPath) { $ModsPath = $ModsPath.Trim() }
}
if (-not $ModsPath) {
    throw "Could not resolve the mods folder. Pass -ModsPath '<Sts2>/mods'."
}
$modSrc = Join-Path $ModsPath $modId
if (-not (Test-Path $modSrc)) {
    throw "Built mod folder not found: $modSrc (build the mod first, or omit -SkipPublish)."
}

# --- Step 2: scaffold the workspace if missing -----------------------------
$seededNew = $false
if (-not (Test-Path $Workspace)) {
    Write-Step "Creating workspace via ModUploader new -w '$Workspace'..."
    & $Uploader new -w $Workspace
    if ($LASTEXITCODE -ne 0) { throw "ModUploader new failed (exit $LASTEXITCODE)." }
    $seededNew = $true
}

$workshopJson = Join-Path $Workspace 'workshop.json'
$contentDir   = Join-Path $Workspace 'content'
$modIdFile    = Join-Path $Workspace 'mod_id.txt'

# Seed workshop.json from the manifest only on first scaffold.
if ($seededNew -and (Test-Path $workshopJson)) {
    Write-Step "Seeding workshop.json from manifest..."
    $deps = @()
    if ($mod.dependencies) { $deps = @($mod.dependencies | ForEach-Object { $_.id }) }
    $ws = [ordered]@{
        title        = $mod.name
        description  = $mod.description
        visibility   = "private"
        changeNote   = "Initial upload"
        tags         = @()
        dependencies = $deps
    }
    $json = $ws | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($workshopJson, $json, (New-Object System.Text.UTF8Encoding($false)))
    Write-Warn2 "Edit $workshopJson (title/description/tags) and replace image.png before your first public upload."
}

# --- Step 3 & 4: stage content/ --------------------------------------------
Write-Step "Staging content/ from $modSrc ..."
$pck = Join-Path $modSrc "$modId.pck"
$dll = Join-Path $modSrc "$modId.dll"
$json = Join-Path $modSrc "$modId.json"
foreach ($f in @($json, $dll, $pck)) {
    if (-not (Test-Path $f)) {
        throw "Required mod file missing: $f`nRun a full 'dotnet publish' first (the .pck only comes from publish, not build)."
    }
}

# Stale check (only meaningful when reusing outputs).
if ($SkipPublish) {
    $pckTime = (Get-Item $pck).LastWriteTimeUtc
    $newestSrc = Get-ChildItem -Path (Join-Path $repoRoot 'TheWickenCode'), (Join-Path $repoRoot 'TheWicken') -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($newestSrc -and $newestSrc.LastWriteTimeUtc -gt $pckTime) {
        Write-Warn2 "Staged .pck ($pckTime UTC) is older than $($newestSrc.Name). Outputs may be stale -- drop -SkipPublish to rebuild."
    }
}

# Rebuild content/ clean.
if (Test-Path $contentDir) { Remove-Item -Path (Join-Path $contentDir '*') -Recurse -Force }
else { New-Item -ItemType Directory -Path $contentDir | Out-Null }

Copy-Item $json -Destination $contentDir
Copy-Item $dll  -Destination $contentDir
Copy-Item $pck  -Destination $contentDir
if ($IncludePdb) {
    $pdb = Join-Path $modSrc "$modId.pdb"
    if (Test-Path $pdb) { Copy-Item $pdb -Destination $contentDir }
    else { Write-Warn2 "-IncludePdb set but $pdb not found; skipping." }
}
Write-Host "    staged:" (Get-ChildItem $contentDir | ForEach-Object { $_.Name }) -ForegroundColor DarkGray

# --- Step 5: patch workshop.json metadata ----------------------------------
if (($ChangeNote -ne "") -or ($Visibility -ne "")) {
    if (-not (Test-Path $workshopJson)) { throw "workshop.json missing: $workshopJson" }
    $ws = Get-Content $workshopJson -Raw | ConvertFrom-Json
    if ($ChangeNote -ne "") { $ws.changeNote = $ChangeNote; Write-Step "changeNote = '$ChangeNote'" }
    if ($Visibility -ne "") { $ws.visibility = $Visibility; Write-Step "visibility = '$Visibility'" }
    $json = $ws | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($workshopJson, $json, (New-Object System.Text.UTF8Encoding($false)))
}

# --- Step 6: sanity checks -------------------------------------------------
$templateImg = Join-Path $uploaderDir 'template/image.png'
$wsImg = Join-Path $Workspace 'image.png'
if ((Test-Path $templateImg) -and (Test-Path $wsImg)) {
    if ((Get-Item $templateImg).Length -eq (Get-Item $wsImg).Length) {
        Write-Warn2 "image.png is still the ModUploader placeholder -- replace $wsImg before a public release."
    }
}
$hasModId = Test-Path $modIdFile

# --- Step 7: upload or print next step -------------------------------------
if ($Upload) {
    if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) {
        throw "Steam is not running. Start Steam (logged in) before uploading."
    }
    if (-not $hasModId) {
        Write-Warn2 "No mod_id.txt found -- this will CREATE A NEW Workshop item. Commit the resulting mod_id.txt afterward."
    }
    Write-Step "Uploading to Steam Workshop..."
    $proc = Start-Process -FilePath $Uploader `
        -ArgumentList @('upload', '-w', "`"$Workspace`"") `
        -WorkingDirectory $uploaderDir -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) { throw "ModUploader upload failed (exit $($proc.ExitCode))." }
    Write-Step "Upload complete."
    if ((-not $hasModId) -and (Test-Path $modIdFile)) {
        Write-Host "    new workshop item id: $(Get-Content $modIdFile -Raw)" -ForegroundColor Green
    }
} else {
    Write-Step "Staged. To upload, run:"
    Write-Host "    ./tools/bundle-workshop.ps1 -Upload" -ForegroundColor Green
    Write-Host "    (or directly: `"$Uploader`" upload -w `"$Workspace`")" -ForegroundColor DarkGray
    if (-not $hasModId) { Write-Warn2 "First upload will create a new Workshop item and write mod_id.txt." }
}
