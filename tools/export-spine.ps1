<#
.SYNOPSIS
    Export a character's Spine projects into the runtime files the mod ships.

.DESCRIPTION
    Reads <project>/<ModId>/animation/raw/spine-export.json and, for each entry whose .spine
    project exists:

      1. Runs the Spine editor CLI: binary skeleton + packed atlas at the entry's atlas scale.
      2. Converts .skel/.atlas to .spskel/.spatlas in a throwaway Godot project that carries the
         game's Spine GDExtension (tools/spine/convert_spine.gd). The game's extension is the
         runtime build and has no .skel/.atlas editor importers, so the mod cannot ship those.
      3. Copies <name>.spskel, <name>.spatlas and the atlas page pngs into
         <project>/<ModId>/animation/<out>/, and writes <name>_skel_data.tres there if it does
         not exist yet (it is never overwritten: animation mixes are tuned in it by hand).
      4. Reports the animations found, any required animation that is missing, and slots that
         are hidden in the editor but still exported.

    Outputs are tracked in git, so building and publishing the mod needs neither Spine nor this
    script. Entries whose .spine project does not exist yet are skipped.

    Needs: a licensed Spine editor (the manifest pins the version), Godot/MegaDot 4.5.x
    (GodotPath in local.props, or -Godot), and gamedata/addons/spine.

.EXAMPLE
    ./tools/export-spine.ps1 -Character Witch
    ./tools/export-spine.ps1 -Character Witch -Only witch_combat -Strict
#>
param(
    [ValidateSet('Witch', 'Augur')]
    [string]$Character = 'Witch',
    # Export only these manifest entries (by name). Default: all.
    [string[]]$Only = @(),
    # Spine CLI executable. Default: $env:SPINE_EXE, then the standard install path.
    [string]$Spine = '',
    # Godot 4.5.x executable. Default: GodotPath from local.props / Directory.Build.props.
    [string]$Godot = '',
    # Fail when a required animation is missing or an editor-hidden slot is exported.
    [switch]$Strict,
    # Keep the temp staging folder for inspection.
    [switch]$KeepStage
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'characters.ps1')
$Char = Get-BirdCharacter $Character

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

# Runs a native tool, returning its merged stdout/stderr as strings. Windows PowerShell wraps
# native stderr lines in ErrorRecords, which would throw under ErrorActionPreference = Stop.
function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = @(& $Exe @Arguments 2>&1 | ForEach-Object { "$_" })
    } finally {
        $ErrorActionPreference = $previous
    }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Lines = $lines }
}

function Resolve-Spine {
    $candidates = @($Spine, $env:SPINE_EXE, (Join-Path $env:ProgramFiles 'Spine\Spine.com')) | Where-Object { $_ }
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }
    throw "Spine CLI not found (tried: $($candidates -join ', ')). Install the Spine editor or pass -Spine."
}

function Resolve-Godot {
    $path = $Godot
    if (-not $path) {
        foreach ($props in 'local.props', 'Directory.Build.props') {
            $file = Join-Path $repoRoot $props
            if (-not (Test-Path $file)) { continue }
            # Skip commented-out examples.
            $text = (Get-Content $file -Raw) -replace '(?s)<!--.*?-->', ''
            if ($text -match '<GodotPath>\s*(.+?)\s*</GodotPath>') { $path = $Matches[1]; break }
        }
    }
    if (-not $path) { throw "Godot not found: set <GodotPath> in local.props (see Directory.Build.props) or pass -Godot." }
    if (-not (Test-Path $path)) { throw "Godot not found at: $path" }
    # The GUI executable does not pipe its output on Windows; prefer the console build beside it.
    $console = $path -replace '\.exe$', '_console.exe'
    if (Test-Path $console) { return $console }
    return $path
}

$animDir = Join-Path $repoRoot (Join-Path $Char.ProjectDir "$($Char.ModId)/animation")
$rawDir = Join-Path $animDir 'raw'
$manifestPath = Join-Path $rawDir 'spine-export.json'
if (-not (Test-Path $manifestPath)) {
    throw "No Spine export manifest for $($Char.Key): $manifestPath"
}
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$entries = @($manifest.exports)
if ($Only.Count -gt 0) {
    $unknown = @($Only | Where-Object { $entries.name -notcontains $_ })
    if ($unknown) { throw "Not in $manifestPath : $($unknown -join ', ')" }
    $entries = @($entries | Where-Object { $Only -contains $_.name })
}

$spineAddon = Join-Path $repoRoot 'gamedata/addons/spine'
if (-not (Test-Path (Join-Path $spineAddon 'spine_godot_extension.gdextension'))) {
    throw "gamedata/addons/spine not found: the conversion needs the game's Spine GDExtension."
}
$spineExe = Resolve-Spine
$godotExe = Resolve-Godot
$convertScript = Join-Path $PSScriptRoot 'spine/convert_spine.gd'

Write-Host "Spine : $spineExe ($($manifest.spineVersion))"
Write-Host "Godot : $godotExe"

# Texture packer settings shared by every export; only the scale differs per entry.
function New-ExportSettings([double]$Scale) {
    return [ordered]@{
        class        = 'export-binary'
        extension    = '.skel'
        open         = $false
        nonessential = $false
        cleanUp      = $false
        packSource   = 'attachments'
        packTarget   = 'perskeleton'
        warnings     = $true
        packAtlas    = [ordered]@{
            stripWhitespaceX = $true; stripWhitespaceY = $true; rotation = $true; alias = $true
            ignoreBlankImages = $false; alphaThreshold = 3
            minWidth = 16; minHeight = 16; maxWidth = 4096; maxHeight = 4096
            pot = $false; multipleOfFour = $false; square = $false
            outputFormat = 'png'; jpegQuality = 0.9
            premultiplyAlpha = $false; bleed = $true; bleedIterations = 2
            scale = @($Scale); scaleSuffix = @(''); scaleResampling = @('bicubic')
            paddingX = 2; paddingY = 2; edgePadding = $true; duplicatePadding = $true
            filterMin = 'Linear'; filterMag = 'Linear'; wrapX = 'ClampToEdge'; wrapY = 'ClampToEdge'
            format = 'RGBA8888'; atlasExtension = '.atlas'
            combineSubdirectories = $false; flattenPaths = $false; useIndexes = $false
            debug = $false; fast = $false; limitMemory = $true; currentProject = $true
            packing = 'polygons'; prettyPrint = $false; legacyOutput = $false
            ignore = $false; separator = '_'; silent = $false
        }
    }
}

$stageRoot = Join-Path ([IO.Path]::GetTempPath()) ("bird-spine-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stageRoot | Out-Null
$utf8 = New-Object Text.UTF8Encoding $false
$results = @()
$problems = @()

try {
    # One throwaway Godot project for all entries, mirroring the mod's res:// layout: a .spatlas
    # finds its page textures relative to the res:// path it was converted at.
    $stageProject = Join-Path $stageRoot 'project'
    New-Item -ItemType Directory -Force (Join-Path $stageProject '.godot') | Out-Null
    [IO.File]::WriteAllText((Join-Path $stageProject 'project.godot'), "config_version=5`n`n[application]`nconfig/name=`"SpineStage`"`n", $utf8)
    Copy-Item $spineAddon (Join-Path $stageProject 'addons/spine') -Recurse
    [IO.File]::WriteAllText((Join-Path $stageProject '.godot/extension_list.cfg'), "res://addons/spine/spine_godot_extension.gdextension`n", $utf8)

    $staged = @()
    foreach ($entry in $entries) {
        $project = Join-Path $rawDir $entry.project
        if (-not (Test-Path $project)) {
            Write-Host "skip:    $($entry.name) ($($entry.project) does not exist yet)" -ForegroundColor DarkGray
            continue
        }

        Write-Step "$($entry.name): Spine export ($($entry.project), atlas scale $($entry.atlasScale))"
        $exportDir = Join-Path $stageRoot "export-$($entry.name)"
        New-Item -ItemType Directory -Force $exportDir | Out-Null
        $settingsPath = Join-Path $stageRoot "$($entry.name).export.json"
        [IO.File]::WriteAllText($settingsPath, (New-ExportSettings $entry.atlasScale | ConvertTo-Json -Depth 5), $utf8)

        $spineRun = Invoke-Native $spineExe @('-u', $manifest.spineVersion, '-i', $project, '-o', $exportDir, '-e', $settingsPath)
        $skels = @(Get-ChildItem $exportDir -Filter '*.skel' -File)
        if ($spineRun.ExitCode -ne 0 -or $skels.Count -eq 0) {
            $spineRun.Lines | ForEach-Object { Write-Host "    $_" }
            throw "Spine export of $($entry.project) failed (exit code $($spineRun.ExitCode))."
        }
        if ($skels.Count -gt 1) {
            throw "$($entry.project) holds $($skels.Count) skeletons ($($skels.BaseName -join ', ')); the pipeline expects one per project."
        }
        $hiddenSlots = @($spineRun.Lines | ForEach-Object { if ($_ -match '^Slot is hidden: \[slot: (.+), bone: .+\]$') { $Matches[1] } })

        # Spine names the files after the skeleton; rename to the manifest name (atlas pages too).
        $skeletonName = $skels[0].BaseName
        $stageDir = Join-Path $stageProject "$($Char.ModId)/animation/$($entry.out)"
        New-Item -ItemType Directory -Force $stageDir | Out-Null
        Copy-Item $skels[0].FullName (Join-Path $stageDir "$($entry.name).skel")
        $atlasText = [IO.File]::ReadAllText((Join-Path $exportDir "$skeletonName.atlas")) -replace "`r`n", "`n"
        $pages = @()
        foreach ($png in Get-ChildItem $exportDir -Filter '*.png' -File) {
            $pageName = $entry.name + $png.Name.Substring($skeletonName.Length)
            $atlasText = [regex]::Replace($atlasText, '(?m)^' + [regex]::Escape($png.Name) + '$', $pageName)
            Copy-Item $png.FullName (Join-Path $stageDir $pageName)
            $pages += $pageName
        }
        [IO.File]::WriteAllText((Join-Path $stageDir "$($entry.name).atlas"), $atlasText, $utf8)

        # Reuse the mod's existing texture import settings (and uid) for pages that already exist.
        $destDir = Join-Path $animDir $entry.out
        foreach ($page in $pages) {
            $existingImport = Join-Path $destDir "$page.import"
            if (Test-Path $existingImport) { Copy-Item $existingImport $stageDir }
        }
        $staged += [pscustomobject]@{ Entry = $entry; StageDir = $stageDir; DestDir = $destDir; Pages = $pages; HiddenSlots = $hiddenSlots }
    }

    if ($staged.Count -gt 0) {
        Write-Step "Importing atlas textures (headless Godot)"
        $importRun = Invoke-Native $godotExe @('--headless', '--path', $stageProject, '--import')
        if ($importRun.ExitCode -ne 0) {
            $importRun.Lines | ForEach-Object { Write-Host "    $_" }
            throw "Godot import of the staging project failed (exit code $($importRun.ExitCode))."
        }
    }

    foreach ($item in $staged) {
        $entry = $item.Entry
        $resBase = "res://$($Char.ModId)/animation/$($entry.out)/$($entry.name)"
        Write-Step "$($entry.name): converting to .spskel/.spatlas"
        $convertRun = Invoke-Native $godotExe @('--headless', '--path', $stageProject, '--script', $convertScript, '--', $resBase)
        $infoLine = $convertRun.Lines | Where-Object { $_ -like 'SPINE_INFO *' } | Select-Object -Last 1
        if ($convertRun.ExitCode -ne 0 -or -not $infoLine) {
            $convertRun.Lines | ForEach-Object { Write-Host "    $_" }
            throw "Converting $($entry.name) failed (exit code $($convertRun.ExitCode))."
        }
        $info = $infoLine.Substring('SPINE_INFO '.Length) | ConvertFrom-Json

        New-Item -ItemType Directory -Force $item.DestDir | Out-Null
        # Drop atlas pages a previous export wrote that this one no longer produces.
        Get-ChildItem $item.DestDir -File |
            Where-Object { $_.Name -match ('^' + [regex]::Escape($entry.name) + '\d*\.png(\.import)?$') -and $item.Pages -notcontains ($_.Name -replace '\.import$', '') } |
            Remove-Item -Force
        foreach ($ext in 'spskel', 'spatlas') {
            Copy-Item (Join-Path $item.StageDir "$($entry.name).$ext") $item.DestDir -Force
        }
        foreach ($page in $item.Pages) {
            Copy-Item (Join-Path $item.StageDir $page) $item.DestDir -Force
            Copy-Item (Join-Path $item.StageDir "$page.import") $item.DestDir -Force
        }

        $skelData = Join-Path $item.DestDir "$($entry.name)_skel_data.tres"
        $createdSkelData = -not (Test-Path $skelData)
        if ($createdSkelData) {
            $tres = @(
                '[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]'
                ''
                "[ext_resource type=`"SpineAtlasResource`" path=`"$resBase.spatlas`" id=`"1_atlas`"]"
                "[ext_resource type=`"SpineSkeletonFileResource`" path=`"$resBase.spskel`" id=`"2_skel`"]"
                ''
                '[resource]'
                'atlas_res = ExtResource("1_atlas")'
                'skeleton_file_res = ExtResource("2_skel")'
                'default_mix = 0.05'
                ''
            ) -join "`n"
            [IO.File]::WriteAllText($skelData, $tres, $utf8)
        }

        $missing = @($entry.requiredAnimations | Where-Object { $info.animations -notcontains $_ })
        $results += [pscustomobject]@{
            Entry = $entry; DestDir = $item.DestDir; Info = $info; Missing = $missing
            HiddenSlots = $item.HiddenSlots; CreatedSkelData = $createdSkelData; Pages = $item.Pages
        }
    }
} finally {
    if ($KeepStage) {
        Write-Host "Stage kept at: $stageRoot"
    } else {
        Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Step "Done."
if ($results.Count -eq 0) {
    Write-Host "  Nothing exported (no .spine project found for the selected entries)."
    exit 0
}
foreach ($result in $results) {
    $name = $result.Entry.name
    $relDest = $result.DestDir.Substring($repoRoot.Length + 1) -replace '\\', '/'
    Write-Host ""
    Write-Host "  $name -> $relDest/" -ForegroundColor Green
    Write-Host "    files      : $name.spskel, $name.spatlas, $($result.Pages -join ', ')"
    if ($result.CreatedSkelData) { Write-Host "    created    : ${name}_skel_data.tres (reference this from the scene's SpineSprite)" }
    Write-Host "    animations : $($result.Info.animations -join ', ')"
    $b = $result.Info.bounds
    Write-Host ("    setup pose : x {0:0}..{1:0}, y {2:0}..{3:0} (skeleton units, y down)" -f $b[0], ($b[0] + $b[2]), $b[1], ($b[1] + $b[3]))
    if ($result.Missing) {
        Write-Warning "$name is missing animations the game plays by name: $($result.Missing -join ', ')"
        $problems += "$name missing animations"
    }
    if ($result.HiddenSlots) {
        Write-Warning "$name has slots hidden in the Spine editor that still export with their setup-pose attachment, so they WILL draw in game: $($result.HiddenSlots -join ', '). Clear the attachment in setup mode instead (key it on in the animations that use it)."
        $problems += "$name exports editor-hidden slots"
    }
}
if ($Strict -and $problems.Count -gt 0) {
    Write-Host ""
    Write-Host "FAILED (-Strict): $($problems -join '; ')" -ForegroundColor Red
    exit 1
}
exit 0
