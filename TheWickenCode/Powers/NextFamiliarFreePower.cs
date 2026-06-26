using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// The player's next familiar Power card (any <see cref="IFamiliarSummon" />) costs 0. Applied by Broom Strike.
/// Mirrors the base game's <c>FreePowerPower</c> pattern: discount via <c>TryModifyEnergyCostInCombatLate</c>,
/// consume in <c>BeforeCardPlayed</c> (which runs before the applying card's own play, so it never self-consumes).
/// </summary>
public sealed class NextFamiliarFreePower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || card is not IFamiliarSummon)
        {
            return false;
        }
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card is IFamiliarSummon
            && cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
