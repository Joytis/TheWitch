using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Cursed Spellbook engine: each turn gain <see cref="PowerModel.Amount" /> extra Energy and lose
/// <see cref="PowerModel.Amount" /> HP. Single-stack — replaying the book refreshes rather than compounding
/// (an upgraded copy raises both sides). Energy hook mirrors the base-game
/// <c>NoEnergyGainPower.ModifyEnergyGain</c>; the HP tick is unblockable/unpowered like Wormy.
/// </summary>
public sealed class CursedSpellbookPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyEnergyGain(Player player, decimal amount) =>
        player == Owner.Player ? amount + Amount : amount;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered, Owner, null);
    }
}
