using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hex (enemy debuff): at the end of the hexed enemy's turn, it suffers one random EVIL effect per stack,
/// rolled from a fixed table — 10 damage, 1 Weak, 1 Vulnerable, steal 1 Strength (enemy loses 1, the hexing
/// witch gains 1), or 6 Poison. Stacks persist (no decrement) — Hex is a geometric scaling engine, not a
/// timed drain. Rolls use the seeded CombatTargets stream, sourced via the hexed creature's combat state so
/// it works without a live applier; the Strength-steal grant just fizzles if the applier is gone.
/// </summary>
public sealed class HexPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const decimal DamageAmount = 10m;
    private const decimal WeakAmount = 1m;
    private const decimal VulnerableAmount = 1m;
    private const decimal StrengthAmount = 1m;
    private const decimal PoisonAmount = 6m;
    private const int EvilEffectCount = 5;

    /// <summary>Hex signature on every application: occult gaze + doom sting on the hexed creature.</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.HexGaze(Owner);
        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Amount <= 0 || !participants.Contains(Owner) || !Owner.IsAlive || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        Rng rng = combat.RunState.Rng.CombatTargets;
        for (int i = 0; i < (int)Amount && Owner.IsAlive; i++)
        {
            await ApplyEvilEffect(choiceContext, rng.NextInt(EvilEffectCount));
        }
    }

    private async Task ApplyEvilEffect(PlayerChoiceContext choiceContext, int roll)
    {
        Creature? witch = Applier is { IsAlive: true } applier ? applier : null;
        switch (roll)
        {
            case 0:
                await CreatureCmd.Damage(choiceContext, [Owner], DamageAmount, ValueProp.Unpowered, witch ?? Owner, null);
                break;
            case 1:
                await PowerCmd.Apply<WeakPower>(choiceContext, Owner, WeakAmount, witch, null);
                break;
            case 2:
                await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, VulnerableAmount, witch, null);
                break;
            case 3:
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -StrengthAmount, witch, null);
                if (witch != null)
                {
                    await PowerCmd.Apply<StrengthPower>(choiceContext, witch, StrengthAmount, witch, null);
                }
                break;
            default:
                await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, PoisonAmount, witch, null);
                break;
        }
    }
}
