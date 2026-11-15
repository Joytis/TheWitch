# Creates NTFS junctions at a character mod's Godot project root pointing into the
# repo's gamedata/ so the Godot editor can resolve base-game res:// paths referenced
# by mod scenes.
# Idempotent: skips links that already exist, errors on real dirs in the way.
# Requires gamedata/ (the local decompiled game source) at the repo root.
# Also junctions the editor-only addons (addons/mega_text, and addons/spine for the Witch).
# No admin needed — junctions don't require Developer Mode.
#
#   ./tools/setup-gamedata-links.ps1                       # the Witch (TheWitch/ Godot project)
#   ./tools/setup-gamedata-links.ps1 -Character Augur    # TheAugur/ Godot project
#   ./tools/setup-gamedata-links.ps1 -Character All

param(
    [ValidateSet('Witch', 'Augur', 'All')]
    [string]$Character = 'Witch'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'characters.ps1')

$gamedata = Join-Path $repoRoot 'gamedata'
if (-not (Test-Path $gamedata)) {
    Write-Error "gamedata/ not found at $repoRoot. Decompile the game there first (see CLAUDE.md)."
}

# Editor-only addons (gitignored + export-excluded via addons/*; the game supplies the runtime).
# mega_text: base-game scenes reference MegaLabel/MegaRichTextLabel. spine: the game's spine-godot
# GDExtension, so Spine nodes/resources load in the Witch editor (Witch only — Spine visuals are Witch-only so far).
$addons = @{
    Witch = @('mega_text', 'spine')
    Augur = @('mega_text')
}

$targets = if ($Character -eq 'All') { $BirdCharacters.Keys } else { @($Character) }
foreach ($key in $targets) {
    $projectDir = Join-Path $repoRoot $BirdCharacters[$key].ProjectDir
    Write-Host "== $key ($projectDir)"
    $names = @('scenes', 'materials', 'shaders', 'images', 'src', 'themes', 'fonts') +
        @($addons[$key] | ForEach-Object { "addons\$_" })
    if ($addons[$key]) { New-Item -ItemType Directory -Force (Join-Path $projectDir 'addons') | Out-Null }
    foreach ($name in $names) {
        $link = Join-Path $projectDir $name
        $target = Join-Path $gamedata $name

        if (-not (Test-Path $target)) {
            Write-Warning "skipped: gamedata\$name does not exist."
            continue
        }

        if (Test-Path $link) {
            $item = Get-Item $link -Force
            if ($item.LinkType -eq 'Junction') {
                if ($item.Target -ne $target) {
                    Write-Error "$link is a junction to '$($item.Target)', expected '$target'. Remove it (rmdir) and re-run."
                }
                Write-Host "ok:      $name -> $($item.Target)"
            } else {
                Write-Error "$link exists and is not a junction. Remove it manually, then re-run."
            }
            continue
        }

        New-Item -ItemType Junction -Path $link -Target $target | Out-Null
        Write-Host "created: $name -> $target"
    }
}
