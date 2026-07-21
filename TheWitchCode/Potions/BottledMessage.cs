using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Bottled Message: card-only (Token rarity) payload potion created by Message in a Bottle, holding the
/// card that was bottled. Using it puts the card back into your hand; hovering it previews the held card.
/// <see cref="BottledCard" /> is mutable-instance state — like every potion, it does NOT survive
/// save/quit (potions serialize id + slot only) or a mid-combat MP rejoin; the potion comes back empty.
/// </summary>
public sealed class BottledMessage : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    /// <summary>The bottled card (combat instance). Stamped by Message in a Bottle on the mutable clone.</summary>
    public CardModel? BottledCard { get; set; }

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        BottledCard is { } card ? [HoverTipFactory.FromCard(card)] : [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (BottledCard is not { } card)
        {
            return;
        }
        BottledCard = null;

        if (card.CombatState == Owner.Creature.CombatState)
        {
            // Same combat it was bottled in: the original card simply comes back.
            await CardPileCmd.Add(card, PileType.Hand);
        }
        else
        {
            // Bottled in an earlier combat — the stored instance belongs to a dead combat state,
            // so a fresh clone enters via the generated path (fires card-creation payoffs).
            await CardPileCmd.AddGeneratedCardToCombat(card.CreateClone(), PileType.Hand, Owner);
        }
    }
}
