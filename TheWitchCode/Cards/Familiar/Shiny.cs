using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Crow familiar token: pocket some gold and a burst of Energy. Exhausts.</summary>
public sealed class Shiny : WitchFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Gold", 10m),
        new DynamicVar("Energy", 2m)
    ];

    public Shiny()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, "vfx/vfx_coin_explosion_small");
        SfxCmd.Play("event:/sfx/ui/gold/gold_1");
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Gold"].UpgradeValueBy(5m);
}
