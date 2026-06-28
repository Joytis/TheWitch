using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Extensions;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Rootcraft: a Power card that grants Bursting Roots — gain Brambles whenever you upgrade a card in hand.</summary>
public sealed class Rootcraft : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(1m)
    ];

    public Rootcraft()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<BurstingRootsPower>(
            choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(1m);
}
