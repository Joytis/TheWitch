using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Rotting Roots: at the start of the owner's turn, every enemy loses <see cref="PowerModel.Amount" /> HP
/// (unblockable, non-attack rot) and gains Hex — 1 per Rotting Roots played. A slow, inevitable decay engine.
/// <para>
/// Two numbers stack independently: <see cref="PowerModel.Amount" /> is the HP loss (3, or 6 from an upgraded
/// card, so it can't double as a play count) and <see cref="_applications" /> is how many times the power was
/// applied, which is the Hex per turn.
/// </para>
/// </summary>
public sealed class RottingRootsPower : WitchPower
{
    // Loc token {HexPerTurn}: the Hex every enemy gains each turn, kept in sync with _applications.
    private const string HexPerTurnKey = "HexPerTurn";

    private int _applications;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(HexPerTurnKey, 1m)];

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Fires for the first application and for every stack added on top of it.
        if (power == this && amount > 0)
        {
            _applications++;
            DynamicVars[HexPerTurnKey].BaseValue = _applications;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in enemies)
        {
            WitchFx.GreenGas(enemy); // rot tick (mirrors base Noxious Fumes; globally preloaded)
        }
        await CreatureCmd.Damage(choiceContext, enemies, Amount, ValueProp.Unblockable | ValueProp.Unpowered, Owner, null, null);
        decimal hex = Math.Max(_applications, 1);
        foreach (Creature enemy in enemies.Where(e => e.IsAlive))
        {
            await PowerCmd.Apply<HexPower>(choiceContext, enemy, hex, Owner, null);
        }
    }
}
