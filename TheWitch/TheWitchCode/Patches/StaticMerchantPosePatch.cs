using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// The Witch's merchant scene (witch_merchant.tscn) is a static sprite until her shop Spine rig is
/// exported: its first child is a Sprite2D tagged with the <see cref="StaticPoseMeta"/> metadata.
/// <see cref="NMerchantCharacter"/> wraps child 0 in a MegaSprite, which throws for anything but a
/// SpineSprite, so its _Ready / PlayAnimation are skipped for a tagged scene. Delete this patch
/// when the scene's first child becomes a SpineSprite.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter))]
public static class StaticMerchantPosePatch
{
    public const string StaticPoseMeta = "witch_static_pose";

    private static bool IsStaticWitch(Node merchant) =>
        merchant.GetChildCount() > 0 && merchant.GetChild(0).HasMeta(StaticPoseMeta);

    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    private static bool SkipReady(NMerchantCharacter __instance) => !IsStaticWitch(__instance);

    [HarmonyPatch(nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    private static bool SkipPlayAnimation(NMerchantCharacter __instance) => !IsStaticWitch(__instance);
}
