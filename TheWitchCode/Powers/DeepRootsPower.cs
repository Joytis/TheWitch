using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Deep Roots: at the start of the owner's turn, gain <see cref="PowerModel.Amount" /> Brambles.
/// Amount is the per-turn yield, so replaying/upgrading the card stacks the growth.
/// </summary>
public sealed class DeepRootsPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
