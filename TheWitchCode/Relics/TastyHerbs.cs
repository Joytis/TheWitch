using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Tasty Herbs: whenever you use an Unstable potion, 25% chance it's used an additional time.
/// The replay goes through the out-of-belt path (<see cref="PotionAutoPlay" /> + a direct <c>OnUse</c>
/// call, the NeverendingPotionPower shape): by the time AfterPotionUsed fires the potion has already
/// been removed from the belt, so <c>OnUseWrapper</c>'s RemoveBeforeUse throws "Tried to remove potion
/// you don't have" — and that exception escaping UsePotionAction froze the action queue for the rest
/// of the combat (AutoSlay seed 55QGFPI, Act 2 boss). Fairy in a Bottle can use the wrapper only because
/// its trigger fires while the potion is still in the belt.
/// </summary>
public sealed class TastyHerbs : WitchRelic
{
    private const float ExtraUseChance = 0.25f;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [UnstablePotions.UnstableHoverTip];

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner
            || !CombatManager.Instance.IsInProgress
            || Owner.Creature.CombatState is not { } combat
            || !UnstablePotions.IsUnstable(potion)
            || Owner.RunState.Rng.Niche.NextFloat() >= ExtraUseChance)
        {
            return;
        }
        Flash();
        await PotionAutoPlay.PlayThrowVfx(potion, Owner.Creature, target, combat);
        // Own choice context: a replayed selection potion must not share the caller's context (one
        // context = one player choice; see the turn-start gotcha in CLAUDE.md).
        HookPlayerChoiceContext replayContext = new(this, LocalContext.NetId.Value, combat, GameActionType.CombatPlayPhaseOnly);
        Task replay = Replay(replayContext, potion, target, Owner);
        if (await replayContext.AssignTaskAndWaitForPauseOrCompletion(replay))
        {
            await replay;
        }
        // else: the potion opened a selection; it finishes in its own queued Play-phase action.
    }

    private static async Task Replay(PlayerChoiceContext replayContext, PotionModel potion, Creature? target, Player player)
    {
        CombatManager.Instance.BeginCardOrPotionEffect(player);
        try
        {
            await (Task)PotionAutoPlay.OnUseMethod.Invoke(potion, [replayContext, target])!;
        }
        finally
        {
            CombatManager.Instance.EndCardOrPotionEffect(player);
        }
    }
}
