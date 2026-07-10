using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Wicker Form: at the start of the owner's turn, gain <see cref="PowerModel.Amount" /> Brambles.
/// The big sibling of <see cref="DeepRootsPower" /> — Amount is the per-turn yield.
/// </summary>
public sealed class WickerFormPower : WitchPower
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
