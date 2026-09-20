using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TheAugur.TheAugurCode.Powers;

namespace TheAugur.TheAugurCode.Commands;

/// <summary>Foretell mechanics entry points (mirrors the base-game <c>OrbCmd</c> shape: the command mutates state, the power/node react).</summary>
public static class ForetellCmd
{
    /// <summary>
    /// Set <paramref name="card"/> aside for <paramref name="turns"/> turns. The card leaves its pile
    /// (so the play pipeline does not move it to a result pile) but stays registered in combat, and
    /// is tracked by the owner's <see cref="ForetellPower"/>, whose stack count = number of Portents.
    ///
    /// Deliberately NOT <c>CardPileCmd.RemoveFromCombat</c>: that also flags
    /// <c>HasBeenRemovedFromState</c>, after which <c>CardPileCmd.Add</c> silently refuses the card and
    /// the later auto-play bails before OnPlay (seen as a "Tried to pop model" stack error).
    /// </summary>
    public static async Task Foretell(PlayerChoiceContext choiceContext, CardModel card, int turns)
    {
        Creature owner = card.Owner.Creature;
        ICombatState? combatState = card.CombatState;
        IRunState runState = card.Owner.RunState;
        PileType oldPile = card.Pile?.Type ?? PileType.None;

        await FlyAway(card);
        card.RemoveFromCurrentPile();
        if (combatState != null)
        {
            await Hook.AfterCardChangedPiles(runState, combatState, card, oldPile, null);
        }

        ForetellPower? power = await PowerCmd.Apply<ForetellPower>(choiceContext, owner, 1m, owner, card);
        power?.Add(card, turns);
    }

    /// <summary>Lift the played card's node off the table toward the Augur and fade it (RemoveFromCombat's node handling, minus the exhaust puff).</summary>
    private static async Task FlyAway(CardModel card)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        NCard? node = room == null ? null : NCard.FindOnTable(card);
        if (room == null || node == null)
        {
            return;
        }
        if (room.Ui.PlayQueue.IsAncestorOf(node))
        {
            room.Ui.PlayQueue.RemoveCardFromQueueForCancellation(node);
        }
        Vector2 globalPosition = node.GlobalPosition;
        node.GetParent()?.RemoveChildSafely(node);
        room.Ui.AddChildSafely(node);
        node.GlobalPosition = globalPosition;

        Tween tween = room.CreateTween().SetParallel();
        tween.TweenProperty(node, "global_position", globalPosition + Vector2.Up * 160f, 0.35f).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(node, "scale", node.Scale * 0.6f, 0.35f).SetEase(Tween.EaseType.In);
        tween.TweenProperty(node, "modulate:a", 0f, 0.35f);
        tween.Play();
        await tween.AwaitFinished(room);
        node.QueueFreeSafely();
    }
}
