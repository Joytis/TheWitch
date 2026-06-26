using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class BearFamiliar : WickenCard, IFamiliarSummon
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Hibernate>(IsUpgraded),
        HoverTipFactory.FromCard<Mutilate>(IsUpgraded),
    ];

    public BearFamiliar()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<BearFamiliarPower>(choiceContext);

        List<WickenFamiliarCard> cards = [];
        cards.AddRange(CreateFamiliarCards<Hibernate>(Owner, 1, CombatState, IsUpgraded));
        cards.AddRange(CreateFamiliarCards<Mutilate>(Owner, 1, CombatState, IsUpgraded));
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }
}
