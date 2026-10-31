using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Familiar token cards render with their summon card's rarity colors (banner, portrait border, type plaque,
/// title outline) — see <see cref="WitchFamiliarCard.DisplayRarity" />. Purely visual: <c>CardModel.Rarity</c>
/// stays Token. Common summons need nothing (Token already renders as Common).
/// </summary>
public static class FamiliarRarityPatch
{
    // NCard reads Model.BannerMaterial for the banner, portrait border and type plaque; that resolves through this getter.
    [HarmonyPatch(typeof(CardModel), "BannerMaterialPath", MethodType.Getter)]
    public static class BannerMaterialPathPatch
    {
        public static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance is not WitchFamiliarCard familiar)
            {
                return;
            }

            switch (familiar.DisplayRarity)
            {
                case CardRarity.Uncommon:
                    __result = "res://materials/cards/banners/card_banner_uncommon_mat.tres";
                    break;
                case CardRarity.Rare:
                    __result = "res://materials/cards/banners/card_banner_rare_mat.tres";
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(NCard), "GetTitleLabelOutlineColor")]
    public static class TitleOutlinePatch
    {
        public static void Postfix(NCard __instance, ref Color __result)
        {
            if (__instance.Model is not WitchFamiliarCard familiar)
            {
                return;
            }

            switch (familiar.DisplayRarity)
            {
                case CardRarity.Uncommon:
                    __result = StsColors.cardTitleOutlineUncommon;
                    break;
                case CardRarity.Rare:
                    __result = StsColors.cardTitleOutlineRare;
                    break;
            }
        }
    }
}
