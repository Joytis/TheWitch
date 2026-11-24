using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Potions;

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
/// <c>RemoveBeforeUse</c> on the already-removed potion and re-fire <c>AfterPotionUsed</c>); each replay is still
/// logged as a potion use in combat history (see <see cref="PotionAutoPlay.ReplayOutOfBelt" />).
/// MP: each replay runs under its OWN <see cref="HookPlayerChoiceContext" /> (the Hook.AfterDeath pattern),
/// NOT the shared turn-start context. The shared context supports exactly one player-choice game action for
/// the whole turn-start window; a second selection (two bottled draft potions, or one plus any other
/// turn-start choice) hits its "Tried to interrupt action" error path and the selection opens unsynced —
/// breaking multiplayer. With a per-potion context, a selection-prompting replay pauses into its own queued
/// CombatPlayPhaseOnly action and resolves at the start of the Play phase, like Mayhem-style effects.
/// Amount mirrors the bottled count for display; the list is plain power state (not DynamicVars), so it does
/// not survive save/reload or sync in MP — same caveat class as FamiliarPower.GrantsUpgradedCards.
/// </summary>
public sealed class NeverendingPotionPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Not readonly: DeepCloneFields must give each mutable clone its own list. MemberwiseClone shares the
    // canonical's list with every clone, so bottled potions would leak into every future combat's instance.
    private List<PotionModel> _bottled = [];

    public void Bottle(PotionModel potion) => _bottled.Add(potion);

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _bottled = [];
    }

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat || !LocalContext.NetId.HasValue)
        {
            return;
        }

        foreach (PotionModel potion in _bottled)
        {
            Creature? target = PotionAutoPlay.ResolveTarget(potion, player, Owner, combat);
            if (potion.TargetType == TargetType.AnyEnemy && target == null)
            {
                continue; // no living enemy this turn — skip, don't drop the bottle
            }

            Flash();
            // Later bottles' instant effects may resolve before a paused selection replay does.
            await PotionAutoPlay.ReplayOutOfBelt(this, potion, target, player, combat);
        }
    }

}
