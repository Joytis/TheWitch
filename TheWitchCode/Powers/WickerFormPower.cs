using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Wicker Form: for each card the owner plays, gain <see cref="PowerModel.Amount" /> Brambles.
/// Fires on the played card's own resolution too (the buff is live once applied, so only later cards count).
/// </summary>
public sealed class WickerFormPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
