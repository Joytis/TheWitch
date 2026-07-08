using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Cat familiar token: feline agility bottled as tempo — gain Energy. Exhausts.</summary>
public sealed class Nimble : WickenFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Energy", 1m)
    ];

    public Nimble()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Energy"].UpgradeValueBy(1m);
}
