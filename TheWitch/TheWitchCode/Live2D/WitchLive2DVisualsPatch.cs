using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// PROTOTYPE. Mounts a Live2D model wherever a Witch scene declares one: a <c>Node2D</c> named
/// <see cref="RootName"/> whose <c>live2d_model</c> metadata names a folder under
/// <c>res://TheWitch/live2d/</c> (<c>witch</c>, <c>witch_rest</c>, <c>witch_shop</c>).
/// Optional <c>live2d_breath_intensity</c> (float, default 1) scales the PLACEHOLDER breathing effect.
/// There is no static sprite: in combat that node IS the creature body (<c>%Visuals</c>, which
/// NCreatureVisuals requires), so every game effect aimed at the body (revive fade, Surrounded
/// flip, Doom reparent) acts on the model directly.
/// The model sits under the root at native canvas size with the canvas bottom-centre on the root's
/// origin, so the root's position/scale in the .tscn is the only placement knob. Place it in the
/// editor against its <see cref="EditorPreviewName"/> child. If Live2D fails to load the Witch is
/// invisible, and the failure is logged as an error.
/// </summary>
internal static class WitchLive2DMount
{
    public const string RootName = "Visuals";

    /// <summary>
    /// Child of the root holding the model's flattened rest pose, drawn exactly where the model's
    /// canvas lands, so the root can be placed in the Godot editor (which doesn't load the Cubism
    /// extension). Editor-only: always removed at runtime, whether or not the model mounts.
    /// </summary>
    public const string EditorPreviewName = "EditorPreview";

    /// <summary>True when <paramref name="scene"/> carries a Live2D mount node.</summary>
    public static bool Declares(Node scene) => FindRoot(scene) != null;

    private static Node2D? FindRoot(Node scene) =>
        scene.FindChild(RootName, recursive: true, owned: false) is Node2D root && root.HasMeta("live2d_model") ? root : null;

    public static void TryMount(Node scene, AnimationTree? tree = null)
    {
        if (FindRoot(scene) is not { } root)
        {
            return;
        }

        if (root.GetNodeOrNull(EditorPreviewName) is { } preview)
        {
            root.RemoveChild(preview);
            preview.QueueFree();
        }

        if (!Live2DRuntime.EnsureLoaded())
        {
            return;
        }

        string? modelJson = Live2DRuntime.FindModelJson(root.GetMeta("live2d_model").AsString());
        if (modelJson != null)
        {
            // PLACEHOLDER breathing (see NWitchLive2D.Attach): per-scene swing scale, e.g. the combat
            // model's metadata/live2d_breath_intensity, which replaced the old Idle scale keyframes.
            float breath = root.GetMeta("live2d_breath_intensity", 1f).AsSingle();
            NWitchLive2D.Attach(root, tree, modelJson, breath);
        }
    }
}

/// <summary>
/// Combat (and every other CreateVisuals caller: game over, unlock screen, fake merchant): runs right
/// after witch_visuals.tscn is instantiated (before _Ready), same shape as
/// <see cref="Monsters.WitchPetVisualsPatch"/>.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
public static class WitchLive2DVisualsPatch
{
    private static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is Witch && __result != null)
        {
            WitchLive2DMount.TryMount(__result, __result.GetNodeOrNull<AnimationTree>("AnimationTree"));
        }
    }
}

/// <summary>Rest site: witch_rest_site.tscn, converted to an NRestSiteCharacter by BaseLib.</summary>
[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class WitchLive2DRestSitePatch
{
    private static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance.Player?.Character is Witch)
        {
            WitchLive2DMount.TryMount(__instance);
        }
    }
}

/// <summary>
/// Merchant: witch_merchant.tscn, converted to an NMerchantCharacter by BaseLib. The node doesn't
/// know its player, so the scene's own <c>live2d_model</c> metadata is the only gate.
///
/// The base _Ready / PlayAnimation wrap child 0 in a MegaSprite, which throws for anything but a
/// SpineSprite. BaseLib's prefixes skip them only for nodes it marks as factory-made, and its
/// PackedScene auto-conversion (how the game's own Instantiate reaches this scene) never sets that
/// mark, so the base body ran and threw ("Expected BoundObject to be a SpineSprite"), which also
/// skipped this postfix: no model, only the editor preview sprite. Skip them for Live2D scenes.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter))]
public static class WitchLive2DMerchantPatch
{
    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    private static bool SkipSpineReady(NMerchantCharacter __instance) => !WitchLive2DMount.Declares(__instance);

    [HarmonyPatch(nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    private static bool SkipSpinePlayAnimation(NMerchantCharacter __instance) => !WitchLive2DMount.Declares(__instance);

    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    [HarmonyPostfix]
    private static void Mount(NMerchantCharacter __instance)
    {
        WitchLive2DMount.TryMount(__instance);
    }
}
