using System;
using System.Linq;
using System.Reflection;
using Bird.Common;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Bird.BirdDebug;

/// <summary>
/// The character the "-bird-*" flags target, resolved lazily from <see cref="BirdCharacterArg.Selected"/>
/// (ModelDb is empty at mod init; resolve at main-menu-ready time or later). Everything the harness
/// used to pin to Witch types goes through here instead:
///   <see cref="Model"/>      the selected CharacterModel (key matched case-insensitively against the
///                            model's CLR type name or its Id.Entry over ModelDb.AllCharacters, which
///                            BaseLib patches to include custom characters).
///   <see cref="Owns"/>       the ASSEMBLY RULE: a card/relic/potion/power "belongs to the selected
///                            character" when its type lives in the same assembly as the character
///                            model — this also covers secondary pools (familiar tokens, status strays).
///   <see cref="ModId"/>      the character mod's manifest id (ModManager mod whose assembly matches;
///                            falls back to the assembly name) — also the res:// root.
///   <see cref="FtuePrefix"/> progress-save FTUE key prefix ("thewitch_"): the model-id prefix of the
///                            character's Id.Entry ("THEWITCH-WITCH" → "thewitch_"), else ModId + "_".
/// </summary>
public static class BirdCharacter
{
    private static CharacterModel? _model;

    public static string Key => BirdCharacterArg.Selected;

    public static CharacterModel Model => _model ??= Resolve();

    public static Assembly Assembly => Model.GetType().Assembly;

    /// <summary>True when <paramref name="model"/>'s type is defined in the selected character's assembly.</summary>
    public static bool Owns(AbstractModel model) => model.GetType().Assembly == Assembly;

    public static string ModId
    {
        get
        {
            Assembly asm = Assembly;
            Mod? mod = ModManager.Mods.FirstOrDefault(m => m.assemblies.Contains(asm));
            return mod?.manifest?.id ?? asm.GetName().Name ?? Key;
        }
    }

    public static string ResPath => $"res://{ModId}";

    public static string FtuePrefix
    {
        get
        {
            string entry = Model.Id.Entry;
            int dash = entry.IndexOf('-');
            return dash > 0
                ? entry[..dash].ToLowerInvariant() + "_"
                : ModId.ToLowerInvariant() + "_";
        }
    }

    private static CharacterModel Resolve()
    {
        string key = Key;
        CharacterModel? found = ModelDb.AllCharacters.FirstOrDefault(c =>
            string.Equals(c.GetType().Name, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Id.Entry, key, StringComparison.OrdinalIgnoreCase));
        if (found == null)
        {
            string known = string.Join(", ", ModelDb.AllCharacters.Select(c => $"{c.GetType().Name} ({c.Id.Entry})"));
            throw new InvalidOperationException($"-{BirdCharacterArg.Arg}={key}: no such character; known: {known}");
        }
        return found;
    }
}
