using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Animal Handling: a burst of familiar tokens — create N random Familiar cards in hand (see <see cref="FamiliarCardRegistry" />).</summary>
public sealed class AnimalHandling : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    public AnimalHandling()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<CardModel> cards = FamiliarCardRegistry.CreateRandom(
            Owner, DynamicVars.Cards.IntValue, CombatState!, Owner.RunState.Rng.CombatCardGeneration, isUpgraded: false);
        List<CardPileAddResult> results = new(cards.Count);
        foreach (CardModel card in cards)
        {
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner));
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
