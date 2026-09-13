using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Moonlight (enemy debuff): at the start of the applier's turn the owner takes <see cref="PowerModel.Amount" />
/// damage (blockable, non-attack, Unpowered so the applier's Strength/Vigor don't re-scale it every turn).
/// Stacks add damage and never decay — Moonbeam applies a big pile, Cloak of Twilight drips it in one at a
/// time. Dealt by the applier so on-damage-dealt payoffs credit the player.
/// </summary>
public sealed class MoonlightPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // Tick on the applier's turn (MP: not every player's); with no applier, on any player's turn start.
        if ((Applier != null && player.Creature != Applier) || Amount <= 0 || !Owner.IsAlive)
        {
            return;
        }

        Creature dealer = Applier ?? Owner;
        Flash();
        WitchFx.Moonbeam(dealer, Owner);
        // No attacker animation on a turn-start tick, so play the cast sound directly (GuidingStar's pattern).
        SfxCmd.Play(WitchFx.CelestialSfx);
        await CreatureCmd.Damage(choiceContext, [Owner], Amount, ValueProp.Unpowered, dealer, null, null);
    }
}
