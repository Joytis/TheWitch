using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;

namespace Bird.Common.QoL;

/// <summary>
/// Right-clicking a belt potion uses it immediately, skipping the Use/Discard popup and (for
/// enemy-targeted potions) the targeting arrow:
/// - enemy-targeted (AnyEnemy) -> thrown at a random living enemy,
/// - player-targeted (AnyPlayer/Self) and untargeted -> used instantly
///   (EnqueueManualUse(null) auto-fills the owner when it's a valid target),
/// - oddballs that genuinely need a pick (TargetedNoCreature merchant throw, AnyAlly) -> fall back
///   to the normal targeting flow, still skipping the popup.
/// Left-click behavior (popup with Use/Discard) is untouched, so Discard stays reachable.
///
/// Shared source: compiled into every mod that includes it (TheWitch gates it to Witch-owned
/// potions, QuickPotions to everyone). Each copy is its own type in its own assembly with its own
/// patch, so with both mods installed every potion holder gets two right-click handlers. They
/// dedupe per click: the first handler that ACTS tags the InputEvent (Godot meta, visible to every
/// assembly) and later handlers skip a tagged event. A handler whose gate rejects the potion leaves
/// the event untagged, so the next one can still act. Order- and version-independent.
///
/// Installed explicitly (<see cref="Install"/>), never by attribute: a mod that compiles this file
/// without calling Install (TheAugur) must not get the behavior through PatchAll.
/// </summary>
public static class PotionQuickUse
{
    /// <summary>Tag on a right-click InputEvent some copy of this patch already acted on. Never rename:
    /// older released copies in other mods check this exact name.</summary>
    private const string HandledMeta = "bird_potion_quick_use";

    private static Func<PotionModel, bool>? _gate;
    private static AccessTools.FieldRef<NPotionHolder, bool>? _isUsableRef;
    private static AccessTools.FieldRef<NPotionHolder, bool>? _disabledRef;

    /// <summary>
    /// Patches <see cref="NPotionHolder"/>. <paramref name="gate"/> picks which potions this mod
    /// quick-uses (checked after the usability rules). Failures are logged, never thrown, so a game
    /// update that breaks this patch costs only quick use, not the calling mod's initialization.
    /// Returns whether the patch is active.
    /// </summary>
    public static bool Install(Harmony harmony, Func<PotionModel, bool> gate, MegaCrit.Sts2.Core.Logging.Logger logger)
    {
        if (_gate != null)
        {
            logger.Error("PotionQuickUse.Install called twice; ignoring the second call.");
            return true;
        }
        try
        {
            // Resolved here (not in static initializers) so a renamed private field fails inside this try.
            _isUsableRef = AccessTools.FieldRefAccess<NPotionHolder, bool>("_isUsable");
            _disabledRef = AccessTools.FieldRefAccess<NPotionHolder, bool>("_disabledUntilPotionRemoved");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(NPotionHolder), "_Ready"),
                postfix: new HarmonyMethod(typeof(PotionQuickUse), nameof(HolderReadyPostfix)));
            _gate = gate;
            return true;
        }
        catch (Exception e)
        {
            logger.Error($"Right-click potion quick use failed to install (game update?): {e}");
            return false;
        }
    }

    private static void HolderReadyPostfix(NPotionHolder __instance)
    {
        __instance.Connect(NClickableControl.SignalName.MouseReleased,
            Callable.From<InputEvent>(evt => OnMouseReleased(__instance, evt)));
    }

    private static void OnMouseReleased(NPotionHolder holder, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right } button
            || button.IsPressed()
            || inputEvent.HasMeta(HandledMeta)
            || !CanQuickUse(holder))
        {
            return;
        }
        inputEvent.SetMeta(HandledMeta, true);
        holder.GetViewport()?.SetInputAsHandled();
        NHoverTipSet.Remove(holder);
        PotionModel potion = holder.Potion!.Model;
        switch (potion.TargetType)
        {
            case TargetType.AnyEnemy:
            {
                Creature owner = potion.Owner.Creature;
                var enemies = owner.CombatState?.GetOpponentsOf(owner).Where(c => c.IsAlive).ToList();
                if (enemies == null || enemies.Count == 0)
                {
                    return;
                }
                UseAt(holder, potion, Rng.Chaotic.NextItem(enemies));
                break;
            }
            case TargetType.TargetedNoCreature:
            case TargetType.AnyAlly:
                // No sensible auto-target; skip the popup but keep the normal targeting flow.
                TaskHelper.RunSafely(UseWithTargeting(holder, potion));
                break;
            default:
                UseAt(holder, potion, null);
                break;
        }
    }

    private static void UseAt(NPotionHolder holder, PotionModel potion, Creature? target)
    {
        holder.DisableUntilPotionRemoved();
        potion.EnqueueManualUse(target);
    }

    private static async Task UseWithTargeting(NPotionHolder holder, PotionModel potion)
    {
        potion.BeforeUse += DisableHolder;
        try
        {
            await holder.UsePotion();
        }
        finally
        {
            potion.BeforeUse -= DisableHolder;
        }
        void DisableHolder() => holder.DisableUntilPotionRemoved();
    }

    /// <summary>Mirrors NPotionPopup._Ready/RefreshButtons gating for the Use button, plus the mod's gate.</summary>
    private static bool CanQuickUse(NPotionHolder holder)
    {
        PotionModel? potion = holder.Potion?.Model;
        if (potion == null || potion.IsQueued || !_isUsableRef!(holder) || _disabledRef!(holder))
        {
            return false;
        }
        if (_gate == null || !_gate(potion))
        {
            return false;
        }
        Creature owner = potion.Owner.Creature;
        if (potion.Owner.RunState.IsGameOver || owner.IsDead || !potion.Owner.CanUseOrRemovePotions
            || !potion.PassesCustomUsabilityCheck)
        {
            return false;
        }
        switch (potion.Usage)
        {
            case PotionUsage.AnyTime:
                return true;
            case PotionUsage.CombatOnly:
                return CombatManager.Instance.IsInProgress
                    && owner.CombatState?.CurrentSide == owner.Side
                    && owner.IsAlive
                    && !CombatManager.Instance.PlayerActionsDisabled
                    && !InACardSelectScreen
                    && NOverlayStack.Instance?.ScreenCount is null or <= 0
                    && NCapstoneContainer.Instance?.InUse != true;
            default:
                return false;
        }
    }

    private static bool InACardSelectScreen =>
        NPlayerHand.Instance?.IsInCardSelection == true
#if STS2_BETA
        || NOverlayStack.Instance?.Peek() is ICardSelectionScreen;   // 0.113: the overlay screens' interface was renamed
#else
        || NOverlayStack.Instance?.Peek() is ICardSelector;
#endif
}
