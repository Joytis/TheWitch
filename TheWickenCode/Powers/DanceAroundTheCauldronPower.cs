using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Dance Around the Cauldron: at the end of the player's turn, create one <see cref="WickedBrew" /> per
/// point of unspent energy, then remove itself (one-shot, this turn only). Energy is still unspent when
/// <c>AfterSideTurnEnd</c> fires (the refill happens at the next turn's start).
/// </summary>
public sealed class DanceAroundTheCauldronPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { PlayerCombatState: { } combat } player)
        {
            return;
        }

        int energy = combat.Energy;
        for (int i = 0; i < energy; i++)
        {
            Flash();
            await PotionCmd.TryToProcure<WickedBrew>(player);
        }

        await PowerCmd.Remove(this);
    }
}
