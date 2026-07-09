using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

public sealed class RatFamiliar : WitchCard, IFamiliarSummon
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<RatFamiliarPower>(),
        HoverTipFactory.FromCard<Plague>(IsUpgraded),
        HoverTipFactory.FromCard<Scavengers>(IsUpgraded),
    ];

    public RatFamiliar()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<RatFamiliarPower>(choiceContext);
    }
}
