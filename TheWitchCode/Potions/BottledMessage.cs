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
/// Bottled Message: card-only (Token rarity) payload potion created by Message in a Bottle. Stores the
/// bottled card's CANONICAL model + upgrade level — never the live combat card (a dead combat's card
/// instance replayed across fights caused an MP desync). Using it creates a fresh copy of that card in
/// your hand via the generated path; hovering previews the held card. State is mutable-instance only —
/// like every potion it does NOT survive save/quit (potions serialize id + slot) or a mid-combat MP
/// rejoin; the potion comes back empty.
/// </summary>
public sealed class BottledMessage : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    private CardModel? _bottledCanonical;
    private int _bottledUpgrades;

    /// <summary>Whether a card is currently bottled.</summary>
    public bool HoldsCard => _bottledCanonical != null;

    /// <summary>Record the bottled card as (canonical model, upgrade level) — deterministic, MP-safe data.</summary>
    public void Bottle(CardModel card)
    {
        AssertMutable();
        _bottledCanonical = card.CanonicalInstance;
        _bottledUpgrades = card.CurrentUpgradeLevel;
    }

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        _bottledCanonical is { } canonical ? [HoverTipFactory.FromCard(canonical, _bottledUpgrades > 0)] : [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (_bottledCanonical is not { } canonical || Owner.Creature.CombatState is not ICombatState combat)
        {
            return;
        }

        CardModel card = combat.CreateCard(canonical, Owner);
        for (int i = 0; i < _bottledUpgrades; i++)
        {
            CardCmd.Upgrade(card);
        }
        _bottledCanonical = null;
        _bottledUpgrades = 0;

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
