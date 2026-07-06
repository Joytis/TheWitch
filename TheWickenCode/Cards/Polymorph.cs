using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Polymorph (was Repurpose): choose a card in your draw pile — it transforms into a random familiar
/// summon Power card (<see cref="IFamiliarSummon" />). Combat-scoped transform via
/// <c>CardCmd.Transform</c>; upgrade sheds Exhaust so it can polymorph every combat turn it's drawn.
/// </summary>
public sealed class Polymorph : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Polymorph()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        CardModel? summon = Owner.RunState.Rng.CombatCardGeneration.NextItem(FamiliarCardRegistry.AllSummonCanonical);
        if (summon == null)
        {
            return;
        }
        CardModel replacement = CombatState!.CreateCard(summon, Owner);
        await CardCmd.Transform(chosen, replacement);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
