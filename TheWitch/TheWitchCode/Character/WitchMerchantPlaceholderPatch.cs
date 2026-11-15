using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// PLACEHOLDER until the Witch's merchant Spine rig exists: witch_merchant.tscn shows a static sprite.
/// The base <see cref="NMerchantCharacter"/> _Ready / PlayAnimation wrap child 0 in a MegaSprite,
/// which throws ("Expected BoundObject to be a SpineSprite") for anything else. BaseLib's own skip
/// only covers factory-made nodes, and the game instantiates this scene through BaseLib's PackedScene
/// auto-conversion, which never sets that mark. So skip both whenever child 0 is not a SpineSprite
/// (the base body would only throw there). Delete this patch once witch_merchant.tscn is Spine.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter))]
public static class WitchMerchantPlaceholderPatch
{
    private static bool HasSpineBody(Node merchant) =>
        merchant.GetChildCount() > 0 && merchant.GetChild(0).GetClass() == "SpineSprite";

    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    private static bool SkipSpineReady(NMerchantCharacter __instance) => HasSpineBody(__instance);

    [HarmonyPatch(nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    private static bool SkipSpinePlayAnimation(NMerchantCharacter __instance) => HasSpineBody(__instance);
}
