using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheAugur.TheAugurCode.Relics;

/// <summary>
/// Starter relic stub: draw an extra card at the start of each combat. Replace once the Augur's
/// identity relic is designed.
/// </summary>
public sealed class AugurStarterRelic : AugurRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), DynamicVars.Cards.BaseValue, Owner);
    }
}
