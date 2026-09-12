using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Pact of Agony buff: the owner's potions deal <see cref="PowerModel.Amount" />% additional damage until end
/// of turn — the base-game Lethality model (Amount is a percent, multiplier = 1 + Amount/100), so repeat plays
/// stack additively: two Pacts = +200% = triple, never 4x.
///
/// Potion damage carries no <c>cardSource</c> and is dealt by the player's own creature, which is
/// indistinguishable from power-dealt damage (Volatile Vapors, Moonbeam) at the ModifyDamage hook. So the
/// window is bracketed instead: <c>BeforePotionUsed</c>/<c>AfterPotionUsed</c> fence the potion's OnUse body
/// (see PotionModel.OnUseWrapper), and only damage dealt inside that window is multiplied. This is the shape
/// the ORIGINAL Eye of Newt power used for its potion-damage bonus (see git history) — the plain instance
/// flag is safe because single-player never restores mid-combat state and MP is deterministic lockstep.
/// Caveat: effects that re-invoke a potion's OnUse OUTSIDE the wrapper — the Eye of Newt fan-out (postfix,
/// runs after AfterPotionUsed) and Neverending Potion's replay — land outside the window and are NOT
/// multiplied.
/// </summary>
public sealed class PotionDamageMultiplierPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>True only while one of the owner's potions is resolving its OnUse body.</summary>
    private bool _resolvingPotion;

    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            _resolvingPotion = true;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            _resolvingPotion = false;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_resolvingPotion || dealer != Owner || cardSource != null)
        {
            return 1m;
        }
        // Volatile Vapors fires off the same potion use/create and would otherwise ride the window.
        if (Owner.GetPower<VolatileVaporsPower>() is { IsDealing: true })
        {
            return 1m;
        }
        Flash();
        return 1m + Amount / 100m;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
