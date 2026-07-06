using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Thirst: whenever the owner draws a card, gain <see cref="PowerModel.Amount" /> Vigor.
/// Amount is the vigor-per-draw, so stacked/upgraded casts compound.
/// </summary>
public sealed class ThirstPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
