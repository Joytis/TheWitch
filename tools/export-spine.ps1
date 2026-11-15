<#
.SYNOPSIS
    Export a character's Spine project(s) into game-loadable spine-godot resources.

.DESCRIPTION
    Per context (combat / rest / shop), from <ModId>/animation/raw/<project>.spine:
      1. Validates the project's animation names against what the game hard-codes
         (missing required = error, missing expected = warning).
      2. Spine CLI export (pinned to the game's runtime version) with raw/<project>.export.json
         (binary .skel, packed atlas, premultiplied alpha OFF) into a temp dir.
      3. Renames the output to the context's file name (Spine names it after the skeleton) and
         copies it into <ModId>/animation/<context>/. Atlas pages that no longer exist are removed.
      4. Headless MegaDot --import (imports the atlas pngs), then tools/spine/convert-spine.gd:
         .skel -> .spskel, .atlas -> .spatlas. The raw .skel/.atlas are deleted afterwards (the
         editor can't import them; the converted files embed their contents).
      5. Writes <name>_skel_data.tres (SpineSkeletonDataResource) if it doesn't exist yet. It is
         never overwritten, so hand-tuned animation mixes survive re-exports.
    Needs the addons/spine junction (tools/setup-gamedata-links.ps1) and a Spine license.
    Outputs (.png + .import, .spskel, .spatlas, _skel_data.tres) are tracked runtime assets: commit them.
    dotnet publish packs them like any other resource; it never runs Spine.

.EXAMPLE
    ./tools/export-spine.ps1                      # Witch, every context
    ./tools/export-spine.ps1 -Context combat
#>
param(
    [ValidateSet('Witch')]
    [string]$Character = 'Witch',
    [string[]]$Context = @(),
    [string]$Spine = 'C:\Program Files\Spine\Spine.com',
    # Must match the game's spine-godot runtime (base-game .skel files are 4.2.43).
    [string]$SpineVersion = '4.2.43',
    [string]$Godot = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'characters.ps1')
$Char = Get-BirdCharacter $Character
$projDir = Join-Path $repoRoot $Char.ProjectDir
$animDir = Join-Path $projDir "$($Char.ModId)/animation"
$rawDir = Join-Path $animDir 'raw'

# Animation names hard-coded by the game (see CharacterModel.GenerateAnimator, NRestSiteCharacter,
# NMerchantCharacter). The merchant scene reuses the COMBAT skeleton like the base game does, so
# relaxed_loop belongs in the combat project.
$CombatRequired = @('idle_loop')
$CombatExpected = @('attack', 'cast', 'hurt', 'die', 'low_health_loop', 'relaxed_loop')
$RestRequired = @('overgrowth_loop', 'hive_loop', 'glory_loop')

# Per-character contexts. Add a row when a new Spine project exists.
$Contexts = @{
    Witch = [ordered]@{
        combat = @{ Project = 'witch_combat'; Name = 'witch_combat'; Required = $CombatRequired; Expected = $CombatExpected }
        # rest = @{ Project = 'witch_rest'; Name = 'witch_rest'; Required = $RestRequired; Expected = @() }
    }
}[$Character]

if ($Context.Count -eq 0) { $Context = @($Contexts.Keys) }
foreach ($c in $Context) {
    if (-not $Contexts.Contains($c)) { throw "Unknown context '$c' for $Character (known: $($Contexts.Keys -join ', '))." }
}

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

function Resolve-Godot([string]$Override) {
    $path = $Override
    if (-not $path) {
        foreach ($props in @('local.props', 'Directory.Build.props')) {
            $file = Join-Path $repoRoot $props
            if ((Test-Path $file) -and ((Get-Content $file -Raw) -match '<GodotPath>\s*(.+?)\s*</GodotPath>')) {
                $path = $Matches[1].Trim()
                break
            }
        }
    }
    if (-not $path -or -not (Test-Path $path)) { throw "Godot not found ('$path'). Set <GodotPath> in local.props or pass -Godot." }
    # The _console build prints to stdout (the GUI build detaches from the terminal).
    $console = $path -replace '\.exe$', '_console.exe'
    if (Test-Path $console) { return $console }
    return $path
}

function Invoke-Godot([string[]]$GodotArgs, [string]$What) {
    # Godot writes routine noise to stderr (e.g. C# TypeLoadExceptions from a stale mod assembly);
    # under 'Stop', Windows PowerShell would turn the first stderr line into a terminating error.
    # Only our script's lines and real errors are echoed; the full output goes to a log on failure.
    $ErrorActionPreference = 'Continue'
    $out = & $script:godotExe @GodotArgs 2>&1 | ForEach-Object { "$_" }
    $out | Where-Object { $_ -match 'convert-spine|^\s*(SCRIPT )?ERROR' -and $_ -notmatch '\.NET' } | ForEach-Object { Write-Host "    $_" }
    if ($LASTEXITCODE -ne 0) {
        $logFile = Join-Path ([IO.Path]::GetTempPath()) 'export-spine-godot.log'
        $out | Set-Content $logFile -Encoding utf8
        Write-Host "    full Godot output: $logFile"
    }
    if ($LASTEXITCODE -ne 0) { throw "$What failed (Godot exit code $LASTEXITCODE)." }
}

if (-not (Test-Path $Spine)) { throw "Spine CLI not found at $Spine (pass -Spine)." }
if (-not (Test-Path (Join-Path $projDir 'addons/spine/spine_godot_extension.gdextension'))) {
    throw "addons/spine missing in $projDir. Run tools/setup-gamedata-links.ps1 -Character $Character."
}
$godotExe = Resolve-Godot $Godot
$convertScript = (Join-Path $PSScriptRoot 'spine/convert-spine.gd') -replace '\\', '/'
$toConvert = @()
$exported = @()

foreach ($c in $Context) {
    $ctx = $Contexts[$c]
    $spineFile = Join-Path $rawDir "$($ctx.Project).spine"
    $settings = Join-Path $rawDir "$($ctx.Project).export.json"
    $outDir = Join-Path $animDir $c
    $name = $ctx.Name
    foreach ($f in @($spineFile, $settings)) { if (-not (Test-Path $f)) { throw "Missing $f" } }

    Write-Step "$c : validate $($ctx.Project).spine"
    $info = & $Spine -u $SpineVersion -i $spineFile 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host $info; throw "Spine project info failed for $spineFile." }
    $skeletons = @([regex]::Matches($info, '(?m)^\s*Skeleton: (.+?)\s*$') | ForEach-Object { $_.Groups[1].Value })
    if ($skeletons.Count -ne 1) { throw "$($ctx.Project).spine must hold exactly one skeleton (found: $($skeletons -join ', '))." }
    $anims = @()
    if ($info -match '(?m)^\s*Animations \(\d+\): (.+?)\s*$') { $anims = $Matches[1] -split ',\s*' }
    Write-Host "    skeleton '$($skeletons[0])', animations: $($anims -join ', ')"
    $missing = @($ctx.Required | Where-Object { $anims -notcontains $_ })
    if ($missing.Count) { throw "$($ctx.Project).spine is missing required animation(s): $($missing -join ', ')." }
    $soft = @($ctx.Expected | Where-Object { $anims -notcontains $_ })
    if ($soft.Count) { Write-Warning "$c : not animated yet (the game logs a warning and holds the current pose): $($soft -join ', ')" }

    Write-Step "$c : Spine $SpineVersion export"
    $tmp = Join-Path ([IO.Path]::GetTempPath()) "export-spine-$Character-$c"
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force -Confirm:$false }
    New-Item -ItemType Directory $tmp | Out-Null
    $log = & $Spine -u $SpineVersion -i $spineFile -o $tmp -e $settings 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host $log; throw "Spine export failed for $spineFile." }
    $log -split "`r?`n" | Where-Object { $_ -match '^(Binary export|Pack|ERROR|WARNING)' } | ForEach-Object { Write-Host "    $_" }

    # Spine names files after the skeleton; rename to the context's file name and patch page names.
    $skelFiles = @(Get-ChildItem $tmp -Filter '*.skel')
    $atlasFiles = @(Get-ChildItem $tmp -Filter '*.atlas')
    if ($skelFiles.Count -ne 1 -or $atlasFiles.Count -ne 1) { throw "Expected one .skel and one .atlas in $tmp." }
    $prefix = [IO.Path]::GetFileNameWithoutExtension($atlasFiles[0].Name)
    $pages = @(Get-ChildItem $tmp -Filter '*.png')
    $pageMap = @{}
    foreach ($p in $pages) { $pageMap[$p.Name] = $name + $p.Name.Substring($prefix.Length) }

    New-Item -ItemType Directory -Force $outDir | Out-Null
    # Drop atlas pages from a previous export that this one no longer produces (+ their .import).
    Get-ChildItem $outDir -File | Where-Object {
        $_.Name -match "^$([regex]::Escape($name))(_\d+)?\.png(\.import)?$" -and $pageMap.Values -notcontains ($_.Name -replace '\.import$', '')
    } | ForEach-Object { Write-Host "    removed stale $($_.Name)"; Remove-Item $_.FullName -Force -Confirm:$false }

    Copy-Item $skelFiles[0].FullName (Join-Path $outDir "$name.skel") -Force
    foreach ($p in $pages) { Copy-Item $p.FullName (Join-Path $outDir $pageMap[$p.Name]) -Force }
    $atlasLines = Get-Content $atlasFiles[0].FullName | ForEach-Object { if ($pageMap.ContainsKey($_.Trim())) { $pageMap[$_.Trim()] } else { $_ } }
    [IO.File]::WriteAllText((Join-Path $outDir "$name.atlas"), (($atlasLines -join "`n") + "`n"))
    Remove-Item $tmp -Recurse -Force -Confirm:$false

    $resDir = "res://$($Char.ModId)/animation/$c"
    $toConvert += "$resDir/$name.skel", "$resDir/$name.atlas"
    $exported += @{ Context = $c; OutDir = $outDir; Name = $name; ResDir = $resDir; Pages = $pageMap.Values }
}

Write-Step "MegaDot: import atlas pages"
Invoke-Godot @('--headless', '--path', $projDir, '--import') 'Godot import'

Write-Step "MegaDot: convert to .spskel / .spatlas"
Invoke-Godot (@('--headless', '--path', $projDir, '-s', $convertScript, '--') + $toConvert) 'Spine conversion'

foreach ($e in $exported) {
    foreach ($ext in @('spskel', 'spatlas')) {
        if (-not (Test-Path (Join-Path $e.OutDir "$($e.Name).$ext"))) { throw "Conversion produced no $($e.Name).$ext" }
    }
    Remove-Item (Join-Path $e.OutDir "$($e.Name).skel"), (Join-Path $e.OutDir "$($e.Name).atlas") -Force -Confirm:$false

    $dataRes = Join-Path $e.OutDir "$($e.Name)_skel_data.tres"
    if (-not (Test-Path $dataRes)) {
        # Base-game shape (animations/characters/silent/silent_skel_data.tres). Mixes are tuned by hand afterwards.
        $tres = @"
[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]

[ext_resource type="SpineAtlasResource" path="$($e.ResDir)/$($e.Name).spatlas" id="1_atlas"]
[ext_resource type="SpineSkeletonFileResource" path="$($e.ResDir)/$($e.Name).spskel" id="2_skel"]

[resource]
atlas_res = ExtResource("1_atlas")
skeleton_file_res = ExtResource("2_skel")
default_mix = 0.05
"@
        [IO.File]::WriteAllText($dataRes, ($tres -replace "`r`n", "`n") + "`n")
        Write-Host "    created $($e.Name)_skel_data.tres"
    }
    Write-Host "$($e.Context): $($e.ResDir)/ -> $($e.Name).spskel, $($e.Name).spatlas, $(@($e.Pages) -join ', ')" -ForegroundColor Green
}
