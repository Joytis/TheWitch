using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Chimera familiar: each turn, adds a random familiar token-card to your hand.</summary>
public sealed class ChimeraFamiliar : WickenCard, IFamiliarSummon
{
    public ChimeraFamiliar()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<ChimeraFamiliarPower>(choiceContext);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
