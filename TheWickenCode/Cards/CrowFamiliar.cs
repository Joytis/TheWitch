using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class CrowFamiliar : WickenCard, IFamiliarSummon
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<ScoutWeakness>(false),
        HoverTipFactory.FromCard<ClawEyes>(false),
        HoverTipFactory.FromCard<Shiny>(false),
    ];

    public CrowFamiliar()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<CrowFamiliarPower>(choiceContext);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
