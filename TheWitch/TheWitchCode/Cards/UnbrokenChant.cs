using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Unbroken Chant: draw a card and play it for free; keep going for as long as the drawn card is a Skill.
/// Stops when the draw fails (empty piles / full hand) or a card would be played a second time — the
/// latter only matters for an all-Skill deck, where played Skills reshuffle back in and it would never end.
/// </summary>
public sealed class UnbrokenChant : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public UnbrokenChant()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        HashSet<CardModel> played = [];
        while (true)
        {
            CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
            if (drawn == null || !played.Add(drawn))
            {
                break;
            }

            await CardCmd.AutoPlay(choiceContext, drawn, null);
            if (drawn.Type != CardType.Skill)
            {
                break;
            }
        }
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
