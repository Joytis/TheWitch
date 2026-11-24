# Shared character-mod table for the tools/ scripts. Dot-source it:
#     . (Join-Path $PSScriptRoot 'characters.ps1')
# then index $BirdCharacters['Witch'] / ['Augur'].
#
# Each character is its own mod (own csproj/assembly, manifest, .pck, mods/ folder, Workshop item)
# in its own Godot project folder (<ProjectDir>/<ModId>/ = assets, <ProjectDir>/<ModId>Code/ = C#). "Bird" is just the arbitrary
# brand for the shared launch-flag family (-bird-debug, -bird-character=..., ...).
#
# Keep in sync with tools/pack-atlases.py CHARACTERS and Docs/card-data/characters.js.

$BirdCharacters = [ordered]@{
    Witch = @{
        Key        = 'Witch'          # value for -bird-character / -Character
        ModId      = 'TheWitch'       # manifest id, res:// root, mods/<ModId>/, assembly name
        ProjectDir = 'TheWitch'       # Godot project root, relative to the repo root
        Csproj     = 'TheWitch/TheWitch.csproj'
        Workshop   = 'TheWitch/workshop'   # Workshop workspace dir, relative to the repo root (the Main item)
        # Optional: a separate Workshop item for the beta game branch's build. Steam's per-branch
        # revision flags on ONE item delivered the beta build to public players (2026-11), so the Witch
        # ships two items again. Its workspace holds only mod_id.txt, workshop.json (title) and
        # description-header.bbcode.txt; the gallery, description body and image.png come from Workshop.
        BetaWorkshop = 'TheWitch/workshop-beta'
        Kind       = 'Character'
        HasPck     = $true
        TagPrefix  = ''                    # release tags are '<TagPrefix><version>'; the Witch predates prefixes
        NotesDir   = 'Docs/patch-notes'    # patch notes: <NotesDir>/<version>.md
    }
    Augur = @{
        Key        = 'Augur'
        ModId      = 'TheAugur'
        ProjectDir = 'TheAugur'
        Csproj     = 'TheAugur/TheAugur.csproj'
        Workshop   = 'TheAugur/workshop'
        Kind       = 'Character'
        HasPck     = $true
        TagPrefix  = 'TheAugur-'
        NotesDir   = 'Docs/patch-notes/TheAugur'
    }
}

# Shipped mods that are NOT characters (code-only quality-of-life mods). Same fields as a character
# entry, so bundle-workshop.ps1 / deploy.ps1 take them via Get-BirdMod; the character-only tools
# (launch, smoke tests, AutoSlay, atlases, Spine, card docs, gamedata junctions) never see them.
$BirdUtilityMods = [ordered]@{
    QuickPotions = @{
        Key        = 'QuickPotions'
        ModId      = 'QuickPotions'
        ProjectDir = 'QuickPotions'
        Csproj     = 'QuickPotions/QuickPotions.csproj'
        Workshop   = 'QuickPotions/workshop'
        Kind       = 'Utility'
        HasPck     = $false
        TagPrefix  = 'QuickPotions-'
        NotesDir   = 'Docs/patch-notes/QuickPotions'
    }
}

# Workshop workspace (repo-relative) a game branch's build is uploaded to: the mod's BetaWorkshop for
# 'beta' when it has one (its own Workshop item), else the one shared Workshop workspace.
function Get-BirdWorkshop($Mod, [string]$Branch) {
    if ($Branch -eq 'beta' -and $Mod.BetaWorkshop) { return $Mod.BetaWorkshop }
    return $Mod.Workshop
}

# The dev-only, character-agnostic debug mod (every -bird-* launch flag: bootstrap, smoke tests,
# FX/Icon Lab). Built alongside whichever character launch.ps1 targets; never bundled to the Workshop.
$BirdDebugCsproj = 'BirdDebug/BirdDebug.csproj'

function Get-BirdCharacter([string]$Key) {
    foreach ($k in $BirdCharacters.Keys) {
        if ($k -ieq $Key) { return $BirdCharacters[$k] }
    }
    throw "Unknown character '$Key' (known: $($BirdCharacters.Keys -join ', '))."
}

# Any shipped mod: a character or a utility mod (for the Workshop / release tools).
function Get-BirdMod([string]$Key) {
    foreach ($table in @($BirdCharacters, $BirdUtilityMods)) {
        foreach ($k in $table.Keys) {
            if ($k -ieq $Key) { return $table[$k] }
        }
    }
    throw "Unknown mod '$Key' (known: $((@($BirdCharacters.Keys) + @($BirdUtilityMods.Keys)) -join ', '))."
}

# Game install folder for a game branch ('public' | 'beta'), as the build resolves it
# (Sts2PathDiscovery.props: local.props override, else game-installs/<branch>/, else for public the
# Steam install). Throws when the folder has no SlayTheSpire2.exe, e.g. beta with no cached copy.
function Get-Sts2BranchPath([string]$Branch) {
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $proj = Join-Path $repoRoot $BirdDebugCsproj
    $path = (& dotnet msbuild $proj -getProperty:Sts2Path "-p:Sts2Branch=$Branch" -nologo 2>$null | Select-Object -Last 1)
    if ($path) { $path = $path.Trim() }
    if (-not $path -or -not (Test-Path (Join-Path $path 'SlayTheSpire2.exe'))) {
        throw "No game install for branch '$Branch' (resolved '$path'). Run tools/setup-game-install.ps1 -Branch $Branch with Steam on that branch."
    }
    return $path
}

# The Steam-managed game install (the one Steam updates and switches between branches).
function Get-SteamSts2Path {
    $candidates = @()
    try {
        $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath
        if ($steam) { $candidates += (Join-Path $steam 'steamapps\common\Slay the Spire 2') }
    } catch {}
    $candidates += 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
    foreach ($c in $candidates) { if (Test-Path (Join-Path $c 'SlayTheSpire2.exe')) { return $c } }
    throw "Could not find the Steam install of Slay the Spire 2. Pass -Sts2Path."
}

# Which game branch Steam has selected for an install: 'public' | 'beta' | $null (unreadable).
# Read from the app manifest beside the library's common/ folder (BetaKey: "public" / "public-beta").
function Get-SteamSts2Branch([string]$Sts2Path) {
    $acf = Join-Path (Split-Path (Split-Path $Sts2Path -Parent) -Parent) 'appmanifest_2868840.acf'
    if (-not (Test-Path $acf)) { return $null }
    $m = [regex]::Match((Get-Content $acf -Raw), '"BetaKey"\s+"([^"]*)"')
    if (-not $m.Success) { return $null }
    if ($m.Groups[1].Value -eq 'public' -or $m.Groups[1].Value -eq '') { return 'public' }
    return 'beta'
}
