using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Trash Diving: dig one card back out of the discard pile (Rummage/base-game Dredge pattern) and a
/// Rats scurries out with it — one Rats is generated straight into your hand. Upgrade sheds Exhaust.
/// </summary>
public sealed class TrashDiving : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    public TrashDiving()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand)
        {
            await CardPileCmd.Add(
                await CardSelectCmd.FromCombatPile(choiceContext, PileType.Discard.GetPile(Owner), Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1)),
                PileType.Hand);
        }

        CardModel rats = CombatState!.CreateCard<Rats>(Owner);
        var generated = await CardPileCmd.AddGeneratedCardToCombat(rats, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(generated);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
