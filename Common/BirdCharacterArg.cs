using System;
using MegaCrit.Sts2.Core.Helpers;

namespace Bird.Common;

/// <summary>
/// The repo-wide <c>-bird-character=&lt;key&gt;</c> launch argument: which character mod the
/// "bird" debug/launch flags (<c>-bird-debug</c>, <c>-bird-bootstrap</c>, ...) are aimed at, so
/// several character mods can be installed at once without all reacting. The key is the
/// character's <c>CharacterId</c> constant ("Witch", "Augur"), case-insensitive.
/// Absent = the Witch (the released mod), so old launch lines keep working.
/// </summary>
public static class BirdCharacterArg
{
    public const string Arg = "bird-character";
    public const string DefaultCharacter = "Witch";

    /// <summary>The selected character key, defaulting to <see cref="DefaultCharacter"/>.</summary>
    public static string Selected
    {
        get
        {
            string? value = CommandLineHelper.GetValue(Arg);
            return string.IsNullOrWhiteSpace(value) ? DefaultCharacter : value.Trim();
        }
    }

    /// <summary>True when the argument (or its default) names <paramref name="characterKey"/>.</summary>
    public static bool IsSelected(string characterKey) =>
        string.Equals(Selected, characterKey, StringComparison.OrdinalIgnoreCase);
}
