using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheAugur.TheAugurCode.Commands;

namespace TheAugur.TheAugurCode.Cards;

/// <summary>
/// Base for every <c>Foretell N</c> card. Playing it (paying its cost) does NOT run the effect: the
/// card leaves every pile and waits as a Portent (see <see cref="Powers.ForetellPower"/>); N turns
/// later the power auto-plays it for free at the start of your turn, which re-enters
/// <see cref="OnPlay"/> with <see cref="ResolvingForetell"/> set and runs <see cref="OnForetold"/>.
///
/// Subclasses declare a <c>new DynamicVar("Foretell", n)</c> in <c>CanonicalVars</c> (referenced as
/// <c>{Foretell}</c> in the description) and implement <see cref="OnForetold"/> instead of OnPlay.
/// Targeted cards get a random hittable enemy at resolution (base-game AutoPlay rule).
/// </summary>
public abstract class ForetellCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : AugurCard(cost, type, rarity, target)
{
    public const string ForetellVarName = "Foretell";

    /// <summary>Set by <see cref="Powers.ForetellPower"/> right before it auto-plays the card; cleared on entry.</summary>
    internal bool ResolvingForetell { get; set; }

    public int ForetellTurns => DynamicVars[ForetellVarName].IntValue;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AugurKeywords.Foretell];

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (ResolvingForetell)
        {
            ResolvingForetell = false;
            await OnForetold(choiceContext, cardPlay);
            return;
        }
        await ForetellCmd.Foretell(choiceContext, this, ForetellTurns);
    }

    /// <summary>The deferred effect. <paramref name="cardPlay"/>.Target is already resolved (random enemy for AnyEnemy cards).</summary>
    protected abstract Task OnForetold(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}
