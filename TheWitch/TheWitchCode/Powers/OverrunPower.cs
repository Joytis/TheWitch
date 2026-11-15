using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Overrun: debuff on the trampled enemy — for the rest of this turn, each Familiar token card the
/// applier plays deals <see cref="PowerModel.Amount" /> damage to it. Repeat plays stack the damage
/// (Counter); one instance per applier so co-op Witches each trample with their own pack.
/// Removed at the end of the owner's turn (base-game Strangle pattern).
/// </summary>
public sealed class OverrunPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Applier?.Player is null || cardPlay.Card.Owner != Applier.Player || cardPlay.Card is not WitchFamiliarCard)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Applier, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
