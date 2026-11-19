using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Bottled Message: card-only (Token rarity) payload potion created by Message in a Bottle. Using it creates a
/// fresh copy of the bottled card in your hand via the generated path; hovering previews that card.
/// <para>
/// The potion's ONLY state is a <see cref="BottledCard" /> — the card as plain data, never the live combat card
/// (a dead combat's card instance replayed across fights caused an MP desync). What the bottle keeps, how it is
/// saved / net-synced and how it becomes a card again all live in that one type.
/// </para>
/// </summary>
public sealed class BottledMessage : WitchTokenPotion
{
    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    // Never mutated once set, so potion clones may share it.
    private BottledCard? _bottled;

    /// <summary>The bottled card, or null if empty. A card that no longer exists (mod update) empties the bottle.</summary>
    internal BottledCard? Bottled
    {
        get => _bottled;
        set
        {
            AssertMutable();
            _bottled = value is { IsKnown: true } ? value : null;
        }
    }

    /// <summary>Whether a card is currently bottled.</summary>
    public bool HoldsCard => _bottled != null;

    /// <summary>Bottle <paramref name="card" /> as it is right now.</summary>
    public void Bottle(CardModel card) => Bottled = BottledCard.Capture(card);

    // Built by the same method as the card that comes out, so the tip cannot disagree with it.
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        _bottled?.CreateCard() is { } preview ? [HoverTipFactory.FromCard(preview)] : [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner.Creature.CombatState is not ICombatState combat || _bottled?.CreateCard(combat, Owner) is not { } card)
        {
            return;
        }

        // Deliberately NOT clearing the bottled card here: a used potion is consumed anyway, and the
        // one surviving reference — this instance held in NeverendingPotionPower._bottled (Crystal
        // Bottle) — replays OnUse every turn start and needs the card to stay for the combat.
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
