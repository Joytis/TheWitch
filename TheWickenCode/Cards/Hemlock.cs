using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Hemlock: your Brambles' retaliation now also seeds the attacker with Hex.</summary>
public sealed class Hemlock : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MegaCrit.Sts2.Core.Models.Powers.BramblesPower>(),
        HoverTipFactory.FromPower<HexPower>(),
    ];

    public Hemlock()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<HemlockPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
