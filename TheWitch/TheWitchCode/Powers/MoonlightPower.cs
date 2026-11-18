using BaseLib.Hooks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Moonlight (enemy debuff): at the end of the enemy turn the owner takes <see cref="PowerModel.Amount" />
/// damage (blockable, non-attack, Unpowered so the applier's Strength/Vigor don't re-scale it every turn).
/// Stacks add damage and never decay — Moonbeam applies a big pile, Cloak of Twilight drips it in one at a
/// time. Dealt by the applier so on-damage-dealt payoffs credit the player.
///
/// Health-bar forecast (BaseLib's <see cref="IHealthBarForecastSource" />, already on CustomPowerModel): BaseLib paints a Poison-style cyan slice on the owner's health
/// bar for the damage the next tick is expected to land (full stacks during the player turn, stacks minus
/// current Block during the enemy turn — Block the enemy gains on its own turn absorbs the tick). The slice wears <c>moonlight_bar_mat.tres</c>
/// (night-sky Doom-style shader); BaseLib owns the layout, so this never fights other mods' forecast bars.
/// </summary>
public sealed class MoonlightPower : WitchPower
{
    private const string BarMaterialPath = MainFile.ResPath + "/Shaders/moonlight_bar_mat.tres";

    /// <summary>Lethal HP-label tint (and flat bar colour if the shader material is missing).</summary>
    private static readonly Color ForecastColor = new("C9DCFF");

    private static Material? _barMaterial;
    private static bool _barMaterialLoadAttempted;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (context.Creature != Owner)
        {
            return [];
        }

        // Block only counts once the enemy turn is under way: Block held during the player turn is cleared
        // at the enemy's turn start, before the tick.
        bool enemyTurn = Owner.CombatState?.CurrentSide == CombatSide.Enemy;
        int damage = enemyTurn ? Math.Max(0, Amount - Math.Max(0, Owner.Block)) : Amount;
        if (damage <= 0)
        {
            return [];
        }

        Material? material = BarMaterial();
        // White overlay modulate: the shader owns the colour, so don't multiply its gradient by the cyan.
        // With no material, fall back to a flat cyan modulate.
        Color? overlayModulate = material != null ? Colors.White : null;
        return HealthBarForecasts
            .FromRight(context, ForecastColor, overlayModulate)
            .Add(damage, HealthBarForecastOrder.ForSideTurnEnd(Owner, CombatSide.Enemy), material)
            .Build();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // One tick per enemy turn, after the enemies have acted (base-game MagicBomb hook shape).
        if (side != CombatSide.Enemy || Amount <= 0 || !Owner.IsAlive || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        Creature dealer = Applier ?? Owner;
        Flash();
        WitchFx.Moonbeam(dealer, Owner);
        // No attacker animation on a turn-end tick, so play the cast sound directly (GuidingStar's pattern).
        SfxCmd.Play(WitchFx.CelestialSfx);
        await CreatureCmd.Damage(choiceContext, [Owner], Amount, ValueProp.Unpowered, dealer, null, null);
    }

    private static Material? BarMaterial()
    {
        if (_barMaterialLoadAttempted)
        {
            return _barMaterial;
        }

        _barMaterialLoadAttempted = true;
        if (!ResourceLoader.Exists(BarMaterialPath))
        {
            MainFile.Logger.Warn($"Moonlight bar material missing at {BarMaterialPath}; using flat cyan.");
            return null;
        }

        _barMaterial = GD.Load<Material>(BarMaterialPath);
        return _barMaterial;
    }
}
