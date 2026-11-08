using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// At the start of the owner's next turn, gain <see cref="MegaCrit.Sts2.Core.Models.PowerModel.Amount" />
/// <see cref="BearCubPower" /> and expire. Same shape as <see cref="StrengthNextTurnPower" /> (Hibernate applies both).
/// </summary>
public sealed class BearCubNextTurnPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BearCubPower>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<BearCubPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}
