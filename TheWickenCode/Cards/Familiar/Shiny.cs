using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Crow familiar token: pocket some gold. Exhausts.</summary>
public sealed class Shiny : WickenFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Gold", 5m)
    ];

    public Shiny()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Gold"].UpgradeValueBy(5m);
}
