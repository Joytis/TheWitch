using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Vile Renewal (on the PLAYER): this turn, Attacks don't remove the owner's Brambles (enemy hits) or the
/// Hex on enemies the owner attacks. Both checks live in <see cref="BramblesPower" /> / <see cref="HexPower" />
/// (the only places those stacks are burned); this power is just the marker. "This turn" must survive the
/// enemy turn — Brambles retaliation fires while monsters attack, after the player turn ends — so it expires
/// at the start of the owner's NEXT turn (the *NextTurn power shape), not at player turn end.
/// </summary>
public sealed class VileRenewalPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}
