using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// The Witch's merchant scene (witch_merchant.tscn) is a static sprite until her shop Spine rig is
/// exported: its first child is a Sprite2D tagged with the <see cref="StaticPoseMeta"/> metadata.
/// <see cref="NMerchantCharacter"/> wraps child 0 in a MegaSprite, which throws for anything but a
/// SpineSprite, so its _Ready / PlayAnimation are skipped for a tagged scene (the tag is looked up
/// down the first-child chain). Delete this patch
/// when the scene's first child becomes a SpineSprite.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter))]
public static class StaticMerchantPosePatch
{
    public const string StaticPoseMeta = "witch_static_pose";

    // BaseLib's scene auto-conversion (Node2D root -> NMerchantCharacter) nests the original root
    // under the generated node, so the tagged sprite is child 0 of child 0 there, not child 0.
    private static bool IsStaticWitch(Node merchant)
    {
        for (Node node = merchant; node.GetChildCount() > 0;)
        {
            node = node.GetChild(0);
            if (node.HasMeta(StaticPoseMeta))
            {
                return true;
            }
        }

        return false;
    }

    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    private static bool SkipReady(NMerchantCharacter __instance) => !IsStaticWitch(__instance);

    [HarmonyPatch(nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    private static bool SkipPlayAnimation(NMerchantCharacter __instance) => !IsStaticWitch(__instance);
}
