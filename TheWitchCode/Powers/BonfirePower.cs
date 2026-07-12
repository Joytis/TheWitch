using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Bonfire: while the owner has at least <see cref="PowerModel.Amount" /> Brambles, cards are played by
/// burning that many Brambles instead of paying Energy. Implemented as a cost hook
/// (<see cref="TryModifyEnergyCostInCombat" /> zeroes the cost while affordable) plus payment in
/// <see cref="BeforeCardPlayed" />. A card whose cost is already 0 (natively or via another effect earlier in
/// the hook chain) burns nothing. <see cref="Amount" /> is the Bramble price (5, or 4 upgraded).
/// </summary>
public sealed class BonfirePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Cards we zeroed at cost-calculation time; consulted (then cleared) when the play actually happens so
    // Brambles are only burned for cards whose Energy this power actually replaced. Transient combat-local
    // bookkeeping — not serialized (single recalculation always precedes the play, so a reload mid-play is safe).
    // Not readonly: DeepCloneFields must give each mutable clone its own set — MemberwiseClone would share the
    // canonical's set across every combat's instance (stale-entry leak; same bug class as NeverendingPotionPower).
    private HashSet<CardModel> _substituted = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _substituted = [];
    }

    private int Brambles => Owner.GetPowerAmount<BramblesPower>();

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner?.Creature == Owner && originalCost > 0 && Brambles >= Amount)
        {
            _substituted.Add(card);
            modifiedCost = 0m;
            return true;
        }

        _substituted.Remove(card);
        modifiedCost = originalCost;
        return false;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!_substituted.Remove(cardPlay.Card) || cardPlay.Card.Owner?.Creature != Owner || Brambles < Amount)
        {
            return;
        }

        Flash();
        if (Owner.GetPower<BramblesPower>() is { } brambles)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), brambles, -Amount, Owner, null);
        }
    }
}
