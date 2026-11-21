<#
.SYNOPSIS
    Bundle the built mod into a Steam Workshop workspace and (optionally) upload it
    via MegaCrit's ModUploader.

.DESCRIPTION
    Produces an upload-ready workspace under <repo>/workshop and stages the
    deployable mod files into workshop/content. By default it first runs
    `dotnet publish` so the .pck is freshly exported, then copies the build
    outputs (<ModId>.json/.dll/.pck) from the game's mods folder into the
    workspace.

    Workspace layout consumed by ModUploader:
        workshop.json   metadata (title, visibility, tags, ...)                [versioned]
        description.bbcode.txt  Workshop description (Steam BBCode)            [versioned]
        image.png       Steam Workshop thumbnail                               [versioned]
        mod_id.txt      published item id, written after first upload          [versioned]
        content/        the mod files that actually get uploaded               [gitignored]
        marketing/      marketing manifest (marketing.json) + its images       [versioned]

    workshop.json and image.png are hand-maintained and never clobbered; this
    script only stages content/ and (optionally) patches named metadata fields.
    The Workshop DESCRIPTION is description.bbcode.txt, pushed by -UpdateMarketing
    (marketing.json "description" names the file), never by ModUploader:
    workshop.json "description" is forced to null so the upload leaves it alone.
    Edit the file, not the Steam web editor: the next sync overwrites web edits.

    Staging is the default. Uploading is opt-in (-Upload) because publishing to
    the Workshop is outward-facing and not trivially reversible.

    After a successful upload, a Discord notification is posted if a webhook is
    configured via $env:DISCORD_WEBHOOK_URL or tools/discord-webhook.local.txt
    (gitignored; file contains just the webhook URL).

.PARAMETER Character
    Which mod to bundle (Witch | Augur | QuickPotions; default Witch; alias -Mod).
    Picks the csproj, manifest, source tree and default workspace
    (tools/characters.ps1 Get-BirdMod). A mod whose manifest has "has_pck": false
    (QuickPotions) is built with `dotnet build` and staged without a .pck.

.PARAMETER SkipPublish
    Skip `dotnet publish`; reuse whatever is already in the mods folder.
    The script warns if the staged .pck looks older than the source tree.

.PARAMETER Upload
    After staging, invoke `ModUploader upload`. Requires Steam to be running and
    logged in. Without this flag the script only stages and prints the command.

.PARAMETER ChangeNote
    Text written to workshop.json "changeNote" before uploading.

.PARAMETER ChangeNoteFile
    Markdown file (e.g. Docs/patch-notes/vX.Y.Z.md) converted to Steam BBCode and used as the
    change note. Ignored when -ChangeNote is given.

.PARAMETER Branch
    Game branch whose build is bundled (public | beta): publishes with -p:Sts2Branch and stages
    from that branch's install (local.props Sts2PublicPath / Sts2BetaPath). '' = default install.

.PARAMETER Visibility
    Override workshop.json "visibility" (private | friends | friendsonly | public).

.PARAMETER RenderDescription
    Convert workshop/description.md to Steam BBCode, print it, and copy it to the
    clipboard for pasting into the Workshop web editor. Does nothing else.

.PARAMETER UpdateMarketing
    Update Marketing Material: sync the Workshop item's gallery (additional preview
    images + YouTube videos, in manifest order) and its description to
    <workspace>/marketing/marketing.json via tools/WorkshopMarketing (ModUploader
    can only set image.png). The description file is checked first (BBCode tags
    balanced, length cap: errors stop the run; every link fetched: warnings). Alone
    it does only that (no publish/stage/upload); with -Upload it runs after the
    upload. Needs mod_id.txt and Steam running + logged in.

.PARAMETER MarketingDryRun
    With -UpdateMarketing (implied): print the gallery + description diff and the
    description checks without submitting.

.PARAMETER IncludePdb
    Also copy the mod's .pdb into content/ (debug symbols). Off by default.

.PARAMETER Workspace
    Workspace directory. Defaults to the character's workshop dir (<repo>/workshop for the Witch).

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

.EXAMPLE
    ./bundle-workshop.ps1 -UpdateMarketing -MarketingDryRun
    # show how the Workshop gallery + description differ from workshop/marketing/marketing.json

.EXAMPLE
    ./bundle-workshop.ps1 -UpdateMarketing
    # only push the gallery (images + videos) and the description; no build, no content upload
#>
[CmdletBinding()]
param(
    [Alias('Mod')]
    [ValidateSet('Witch','Augur','QuickPotions')]
    [string]$Character = 'Witch',
    [switch]$SkipPublish,
    [switch]$Upload,
    [string]$ChangeNote = "",
    [string]$ChangeNoteFile = "",
    [ValidateSet('','public','beta')]
    [string]$Branch = '',
    [switch]$RenderDescription,
    [switch]$UpdateMarketing,
    [switch]$MarketingDryRun,
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
. (Join-Path $PSScriptRoot 'characters.ps1')
$Char     = Get-BirdMod $Character
$csproj   = Join-Path $repoRoot $Char.Csproj
$manifest = Join-Path $repoRoot (Join-Path $Char.ProjectDir "$($Char.ModId).json")

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn2($msg) { Write-Host "[warn] $msg" -ForegroundColor Yellow }

# Convert a small markdown subset to Steam Workshop BBCode.
# Supported: # / ## / ### headings, - or * bullet lists, --- rules,
# **bold**, *italic*, `code` (as bold), [text](http-url) links.
function Convert-MarkdownToBBCode([string]$md) {
    $md = $md -replace "`r`n", "`n"
    $out = New-Object System.Collections.Generic.List[string]
    $inList = $false
    foreach ($line in ($md -split "`n")) {
        if ($line -match '^\s*[-*]\s+(.*)$') {
            if (-not $inList) { $out.Add('[list]'); $inList = $true }
            $out.Add('[*]' + $Matches[1])
            continue
        }
        if ($inList) { $out.Add('[/list]'); $inList = $false }
        if     ($line -match '^###\s+(.*)$') { $out.Add('[h3]' + $Matches[1] + '[/h3]') }
        elseif ($line -match '^##\s+(.*)$')  { $out.Add('[h2]' + $Matches[1] + '[/h2]') }
        elseif ($line -match '^#\s+(.*)$')   { $out.Add('[h1]' + $Matches[1] + '[/h1]') }
        elseif ($line -match '^\s*(---+|___+)\s*$') { $out.Add('[hr][/hr]') }
        else { $out.Add($line) }
    }
    if ($inList) { $out.Add('[/list]') }
    $text = $out -join "`n"
    # Links before bold/italic so bracketed BBCode tags can't be mistaken for link text.
    $text = [regex]::Replace($text, '\[([^\]]+)\]\((https?://[^)\s]+)\)', '[url=$2]$1[/url]')
    $text = [regex]::Replace($text, '\*\*(.+?)\*\*', '[b]$1[/b]')
    $text = [regex]::Replace($text, '(?<!\*)\*([^*\n]+)\*(?!\*)', '[i]$1[/i]')
    $text = [regex]::Replace($text, '`([^`\n]+)`', '[b]$1[/b]')
    return $text.Trim()
}

# --- Read the mod manifest -------------------------------------------------
if (-not (Test-Path $manifest)) { throw "Mod manifest not found: $manifest" }
$mod = Get-Content $manifest -Raw | ConvertFrom-Json
$modId = $mod.id
if (-not $modId) { throw "Manifest has no 'id': $manifest" }
$hasPck = [bool]$mod.has_pck

# --- Resolve the ModUploader executable ------------------------------------
if (-not $Uploader) {
    $Uploader = Join-Path $PSScriptRoot 'ModUploader-win-x64/ModUploader.exe'
}
if (-not (Test-Path $Uploader)) { throw "ModUploader.exe not found: $Uploader" }
$uploaderDir = Split-Path -Parent $Uploader

# --- Resolve the workspace -------------------------------------------------
if (-not $Workspace) { $Workspace = Join-Path $repoRoot $Char.Workshop }

# --- -RenderDescription: markdown draft -> BBCode for the Steam web editor ---
if ($RenderDescription) {
    $descriptionMd = Join-Path $Workspace 'description.md'
    if (-not (Test-Path $descriptionMd)) { throw "description.md not found: $descriptionMd" }
    $bb = Convert-MarkdownToBBCode ([System.IO.File]::ReadAllText($descriptionMd, [System.Text.Encoding]::UTF8))
    Write-Output $bb
    try { Set-Clipboard -Value $bb; Write-Warn2 "BBCode copied to clipboard. Paste into the Workshop item's description editor (Steam does not push it)." }
    catch { Write-Warn2 "Clipboard unavailable; copy the output above." }
    if ($bb -match '!!NEED IMAGE HERE!!') { Write-Warn2 "Draft still contains '!!NEED IMAGE HERE!!' placeholders." }
    exit 0
}

# --- Update Marketing Material: Workshop gallery + description from marketing/marketing.json ---
# ModUploader only sets the main preview (image.png); the gallery images + YouTube videos and the
# description go through tools/WorkshopMarketing, which diffs the live item against the manifest and only
# submits what changed. Runs standalone (no publish/stage/upload) or after -Upload.
if ($MarketingDryRun) { $UpdateMarketing = $true }
function Update-WorkshopMarketing {
    $marketingJson = Join-Path $Workspace 'marketing/marketing.json'
    if (-not (Test-Path $marketingJson)) { throw "Marketing manifest not found: $marketingJson" }
    $idFile = Join-Path $Workspace 'mod_id.txt'
    if (-not (Test-Path $idFile)) { throw "No mod_id.txt in $Workspace -- upload the item once (-Upload) before updating its gallery." }
    $itemId = (Get-Content $idFile -Raw).Trim()
    if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) {
        throw "Steam is not running. Start Steam (logged in) before updating marketing material."
    }

    $toolProj = Join-Path $PSScriptRoot 'WorkshopMarketing/WorkshopMarketing.csproj'
    $toolOut  = Join-Path $PSScriptRoot 'WorkshopMarketing/bin/out'
    Write-Step "Building WorkshopMarketing..."
    & dotnet build $toolProj -c Release -o $toolOut -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "WorkshopMarketing build failed (exit $LASTEXITCODE)." }

    $toolArgs = @('--item', $itemId, '--manifest', $marketingJson)
    if ($MarketingDryRun) { $toolArgs += '--dry-run' }
    Write-Step "Updating marketing material (Workshop gallery + description) for item $itemId..."
    & (Join-Path $toolOut 'WorkshopMarketing.exe') @toolArgs
    if ($LASTEXITCODE -ne 0) { throw "WorkshopMarketing failed (exit $LASTEXITCODE)." }
}
if ($UpdateMarketing -and -not $Upload) {
    Update-WorkshopMarketing
    exit 0
}

# --- Change note from a markdown file (patch notes) ------------------------
if ((-not $ChangeNote) -and $ChangeNoteFile) {
    if (-not (Test-Path $ChangeNoteFile)) { throw "Change note file not found: $ChangeNoteFile" }
    $ChangeNote = Convert-MarkdownToBBCode ([System.IO.File]::ReadAllText((Resolve-Path $ChangeNoteFile).Path, [System.Text.Encoding]::UTF8))
}

# --- Step 1: publish (default) ---------------------------------------------
$branchProps = @()
if ($Branch) { $branchProps = @("-p:Sts2Branch=$Branch"); Write-Step "Game branch: $Branch" }
if ($SkipPublish) {
    Write-Step "Skipping publish (-SkipPublish); reusing existing mods/ outputs."
} elseif ($hasPck) {
    Write-Step "Publishing ($csproj) -- this exports the .pck via headless Godot..."
    & dotnet publish $csproj @branchProps
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }
} else {
    Write-Step "Building ($csproj) -- code-only mod, no .pck..."
    & dotnet build $csproj @branchProps
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)." }
}

# --- Resolve the mods source folder ----------------------------------------
if (-not $ModsPath) {
    Write-Step "Resolving mods path from build..."
    $ModsPath = (& dotnet msbuild $csproj -getProperty:ModsPath @branchProps -nologo 2>$null | Select-Object -Last 1)
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
        description  = $null   # pushed by -UpdateMarketing (WorkshopMarketing); null = unchanged
        visibility   = "private"
        changeNote   = "Initial upload"
        tags         = @()
        dependencies = $deps
    }
    $json = $ws | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($workshopJson, $json, (New-Object System.Text.UTF8Encoding($false)))
    Write-Warn2 "Edit $workshopJson (title/tags) and replace image.png before your first public upload."
}

# --- Step 3 & 4: stage content/ --------------------------------------------
Write-Step "Staging content/ from $modSrc ..."
$pck = Join-Path $modSrc "$modId.pck"
$dll = Join-Path $modSrc "$modId.dll"
$json = Join-Path $modSrc "$modId.json"
$stageFiles = @($json, $dll)
if ($hasPck) { $stageFiles += $pck }
foreach ($f in $stageFiles) {
    if (-not (Test-Path $f)) {
        $how = if ($hasPck) { "Run a full 'dotnet publish' first (the .pck only comes from publish, not build)." } else { "Run 'dotnet build $($Char.Csproj)' first." }
        throw "Required mod file missing: $f`n$how"
    }
}

# Stale check (only meaningful when reusing outputs).
if ($SkipPublish) {
    $artifact = if ($hasPck) { $pck } else { $dll }
    $artifactTime = (Get-Item $artifact).LastWriteTimeUtc
    $srcRoot = Join-Path $repoRoot $Char.ProjectDir
    $srcDirs = @((Join-Path $srcRoot "$($Char.ModId)Code"), (Join-Path $srcRoot $Char.ModId), (Join-Path $repoRoot 'Common'))
    $newestSrc = Get-ChildItem -Path $srcDirs -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($newestSrc -and $newestSrc.LastWriteTimeUtc -gt $artifactTime) {
        Write-Warn2 "Staged $(Split-Path $artifact -Leaf) ($artifactTime UTC) is older than $($newestSrc.Name). Outputs may be stale -- drop -SkipPublish to rebuild."
    }
}

# Rebuild content/ clean.
if (Test-Path $contentDir) { Remove-Item -Path (Join-Path $contentDir '*') -Recurse -Force }
else { New-Item -ItemType Directory -Path $contentDir | Out-Null }

foreach ($f in $stageFiles) { Copy-Item $f -Destination $contentDir }

# Keep the staged manifest's version in sync with the repo manifest (matters
# for -SkipPublish -Upload, where the mods-folder copy predates the bump).
$stagedJson = Join-Path $contentDir "$modId.json"
$staged = Get-Content $stagedJson -Raw | ConvertFrom-Json
if ($staged.version -ne $mod.version) {
    Write-Step "Syncing staged manifest version -> $($mod.version)"
    $raw = Get-Content $stagedJson -Raw
    $raw = $raw -replace ('"version":\s*"' + [regex]::Escape($staged.version) + '"'), ('"version": "' + $mod.version + '"')
    [System.IO.File]::WriteAllText($stagedJson, $raw, (New-Object System.Text.UTF8Encoding($false)))
}
if ($IncludePdb) {
    $pdb = Join-Path $modSrc "$modId.pdb"
    if (Test-Path $pdb) { Copy-Item $pdb -Destination $contentDir }
    else { Write-Warn2 "-IncludePdb set but $pdb not found; skipping." }
}
Write-Host "    staged:" (Get-ChildItem $contentDir | ForEach-Object { $_.Name }) -ForegroundColor DarkGray

# --- Step 5: patch workshop.json metadata ----------------------------------
# changeNote is always rewritten on upload runs: workshop.json is versioned, so
# leaving it untouched would resend the PREVIOUS upload's note as this update's
# patch notes. No -ChangeNote => blank note (no patch-note text on Steam).
# The description is never pushed through ModUploader (-UpdateMarketing owns it, see header):
# a non-null "description" in workshop.json would overwrite the live page with stale text.
$forceNullDesc = $false
if (Test-Path $workshopJson) {
    $probe = Get-Content $workshopJson -Raw | ConvertFrom-Json
    $forceNullDesc = ($null -ne $probe.PSObject.Properties['description']) -and ($null -ne $probe.description)
}
if (($ChangeNote -ne "") -or ($Visibility -ne "") -or $Upload -or $forceNullDesc) {
    if (-not (Test-Path $workshopJson)) { throw "workshop.json missing: $workshopJson" }
    $ws = Get-Content $workshopJson -Raw | ConvertFrom-Json
    if ($forceNullDesc) {
        $ws.description = $null
        Write-Warn2 "workshop.json had a non-null description; set to null so ModUploader does not overwrite the description (description.bbcode.txt, synced by -UpdateMarketing)."
    }
    if ($ChangeNote -ne "") { $ws.changeNote = $ChangeNote; Write-Step "changeNote = '$ChangeNote'" }
    elseif ($Upload -and $ws.changeNote -ne "") {
        $ws.changeNote = ""
        Write-Warn2 "No -ChangeNote given; clearing stale changeNote so it isn't resent as this update's patch notes."
    }
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

    # --- Step 8: Discord notification ----------------------------------------
    # Webhook URL comes from $env:DISCORD_WEBHOOK_URL or tools/discord-webhook.local.txt
    # (gitignored). If neither is set, skip silently; a failed POST warns but
    # never fails the upload.
    $webhookUrl = $env:DISCORD_WEBHOOK_URL
    $webhookFile = Join-Path $PSScriptRoot 'discord-webhook.local.txt'
    if ((-not $webhookUrl) -and (Test-Path $webhookFile)) {
        $webhookUrl = (Get-Content $webhookFile -Raw).Trim()
    }
    if ($webhookUrl) {
        Write-Step "Posting update notification to Discord..."
        $ws = Get-Content $workshopJson -Raw | ConvertFrom-Json
        $note = if ($ws.changeNote) { $ws.changeNote } else { "(no change note)" }
        $desc = "**Change note:** $note`n**Version:** $($mod.version)"
        if (Test-Path $modIdFile) {
            $itemId = (Get-Content $modIdFile -Raw).Trim()
            $desc += "`n[View on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=$itemId)"
        }
        $payload = @{
            embeds = @(@{
                title       = "$($ws.title) updated on the Steam Workshop"
                description = $desc
                color       = 9534463
            })
        } | ConvertTo-Json -Depth 10
        try {
            Invoke-RestMethod -Uri $webhookUrl -Method Post -ContentType 'application/json' -Body $payload | Out-Null
            Write-Host "    Discord notified." -ForegroundColor Green
        } catch {
            Write-Warn2 "Discord notification failed: $($_.Exception.Message)"
        }
    } else {
        Write-Warn2 "No Discord webhook configured (set DISCORD_WEBHOOK_URL or create tools/discord-webhook.local.txt); skipping notification."
    }

    # --- Step 9: marketing material (after the notification, so a gallery failure never skips it) ---
    if ($UpdateMarketing) { Update-WorkshopMarketing }
} else {
    Write-Step "Staged. To upload, run:"
    Write-Host "    ./tools/bundle-workshop.ps1 -Character $($Char.Key) -Upload" -ForegroundColor Green
    Write-Host "    (or directly: `"$Uploader`" upload -w `"$Workspace`")" -ForegroundColor DarkGray
    if (-not $hasModId) { Write-Warn2 "First upload will create a new Workshop item and write mod_id.txt." }
}
