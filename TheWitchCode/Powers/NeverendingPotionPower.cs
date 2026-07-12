using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Neverending Potion: holds the consumed potion instances captured by <see cref="CrystalBottlePower" /> and
/// replays each one's effect at the start of the player's turn (in the AutoPrePlay phase — the game's hook for
/// turn-start effects that may auto-play cards, e.g. a bottled Distilled Chaos; replaying in AfterPlayerTurnStart
/// ran during the Start phase and locked the turn: the phase never advanced to Play, so no cards could be
/// played). Replay targeting: single-enemy potions hit a
/// random living enemy (re-rolled each turn); self/player potions target the owner; everything else
/// (AllEnemies/None) passes no target and lets the potion's own OnUse fan out. Replays invoke the protected
/// <c>PotionModel.OnUse</c> directly via reflection (<c>OnUseWrapper</c> would throw in
/// <c>RemoveBeforeUse</c> on the already-removed potion and re-fire <c>AfterPotionUsed</c>). The real
/// turn-start PlayerChoiceContext is passed through, so a potion that prompts a selection resolves normally.
/// Amount mirrors the bottled count for display; the list is plain power state (not DynamicVars), so it does
/// not survive save/reload or sync in MP — same caveat class as FamiliarPower.GrantsUpgradedCards.
/// </summary>
public sealed class NeverendingPotionPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private static readonly MethodInfo OnUseMethod = AccessTools.Method(typeof(PotionModel), "OnUse");

    private readonly List<PotionModel> _bottled = [];

    public void Bottle(PotionModel potion) => _bottled.Add(potion);

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        foreach (PotionModel potion in _bottled)
        {
            Creature? target = ResolveTarget(potion, player, combat);
            if (potion.TargetType == TargetType.AnyEnemy && target == null)
            {
                continue; // no living enemy this turn — skip, don't drop the bottle
            }

            Flash();
            CombatManager.Instance.BeginCardOrPotionEffect(player);
            try
            {
                await (Task)OnUseMethod.Invoke(potion, [choiceContext, target])!;
            }
            finally
            {
                CombatManager.Instance.EndCardOrPotionEffect(player);
            }
        }
    }

    private Creature? ResolveTarget(PotionModel potion, Player player, ICombatState combat) => potion.TargetType switch
    {
        TargetType.AnyEnemy => player.RunState.Rng.CombatTargets.NextItem(
            combat.HittableEnemies.Where(e => e.IsAlive).ToList()),
        TargetType.Self or TargetType.AnyPlayer or TargetType.AnyAlly => Owner,
        _ => null,
    };
}
