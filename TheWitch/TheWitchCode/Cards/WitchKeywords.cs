using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// The Witch's custom card keywords. BaseLib generates the enum values at load and maps each to the
/// loc key <c>THEWITCH-&lt;FIELD&gt;</c> in card_keywords.json (title + description = the hover tip).
/// </summary>
public static class WitchKeywords
{
    /// <summary>
    /// Familiar: marks the token cards a familiar adds each turn (every <see cref="WitchFamiliarCard" />), so
    /// "Familiar card" payoffs (Overrun, Animal Handling) are readable on the card face. Its hover tip doubles as
    /// the shared familiar rule (<see cref="Powers.Familiars.HoverTip" />) shown on summon cards and payoffs.
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Familiar;
}
