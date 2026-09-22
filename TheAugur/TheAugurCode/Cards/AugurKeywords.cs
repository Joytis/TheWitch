using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TheAugur.TheAugurCode.Cards;

/// <summary>
/// The Augur's custom card keywords. BaseLib generates the enum values at load and maps each to the
/// loc key <c>THEAUGUR-&lt;FIELD&gt;</c> in card_keywords.json (title + description = the hover tip).
/// </summary>
public static class AugurKeywords
{
    /// <summary>
    /// Foretell N: when played, the card leaves every pile and waits as a Portent over the Augur's
    /// head; N turns later it is auto-played for free at the start of your turn (random target).
    /// The "N" is written into each card's own description via its <c>Foretell</c> dynamic var;
    /// the keyword only supplies the hover tip (AutoKeywordPosition.None).
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Foretell;
}
