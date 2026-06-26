using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

public sealed class SerratedBones : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(10m)
    ];

    public SerratedBones()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(3m);
}
