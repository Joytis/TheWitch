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
        Workshop   = 'TheWitch/workshop'   # Workshop workspace dir, relative to the repo root
    }
    Augur = @{
        Key        = 'Augur'
        ModId      = 'TheAugur'
        ProjectDir = 'TheAugur'
        Csproj     = 'TheAugur/TheAugur.csproj'
        Workshop   = 'TheAugur/workshop'
    }
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
