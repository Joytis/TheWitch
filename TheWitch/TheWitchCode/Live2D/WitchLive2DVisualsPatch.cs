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

    public static void TryMount(Node scene, AnimationTree? tree = null)
    {
        if (scene.FindChild(RootName, recursive: true, owned: false) is not Node2D root
            || !root.HasMeta("live2d_model"))
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
            NWitchLive2D.Attach(root, tree, modelJson);
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
/// know its player, so the scene's own Live2DRoot metadata is the only gate. Runs even when
/// BaseLib's prefix skips the Spine-only _Ready body.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
public static class WitchLive2DMerchantPatch
{
    private static void Postfix(NMerchantCharacter __instance)
    {
        WitchLive2DMount.TryMount(__instance);
    }
}
