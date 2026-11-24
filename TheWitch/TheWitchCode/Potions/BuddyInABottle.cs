using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Buddies in a Bottle (class name kept from "Buddy in a Bottle" so saved belts still resolve): add
/// <see cref="Count" /> different random Familiar summon cards to your hand, free to play this turn. (A potion
/// can't "play" a card, so it delivers the summon cards free rather than summoning directly.)
/// </summary>
public sealed class BuddyInABottle : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    private const int Count = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(Count)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        List<CardModel> pool = FamiliarCardRegistry.AllSummonCanonical(Owner.RunState).ToList();
        for (int i = 0; i < DynamicVars.Cards.IntValue && pool.Count > 0; i++)
        {
            CardModel canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(pool)!;
            pool.Remove(canonical);
            CardModel summon = Owner.Creature.CombatState!.CreateCard(canonical, Owner);
            summon.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(summon, PileType.Hand, Owner);
        }
    }
}
