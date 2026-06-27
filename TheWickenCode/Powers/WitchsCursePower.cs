using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Witch's Curse (applied to an enemy): while a potion is being used, this creature takes DOUBLE damage
/// from it. Potion damage carries no card/potion source, so it can't be spotted in the damage pipeline
/// directly — instead we flag the window between <see cref="BeforePotionUsed" /> and
/// <see cref="AfterPotionUsed" />, which bracket the potion's <c>OnUse</c> body (where its damage lands).
/// Removes itself at the end of the player's turn ("this turn"). Unlike Vulnerable, it does NOT gate on
/// <c>IsPoweredAttack</c> — potion damage is <see cref="ValueProp.Unpowered" />.
/// </summary>
public sealed class WitchsCursePower : WickenPower
{
    private bool _potionInUse;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        _potionInUse = true;
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        _potionInUse = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        _potionInUse && target == Owner ? 2m : 1m;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
