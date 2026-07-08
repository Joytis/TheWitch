using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Hijacks creature-visual creation for <see cref="WitchPet" />s: the pet reuses the base
/// game's textureless "rocket" host scene (a valid NCreatureVisuals), and this postfix swaps
/// our own sprite onto its %Visuals body. Lets us give pets custom art by shipping only a PNG,
/// sidestepping the (mod-impossible) authoring/packaging of a custom creature_visuals scene.
///
/// Runs right after the scene is instantiated, before it enters the tree / _Ready, so the
/// game's NCreatureVisuals._Ready then picks up our modified %Visuals as its body.
/// CreateVisuals isn't virtual, hence the Harmony patch.
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class WitchPetVisualsPatch
{
    private static void Postfix(MonsterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not WitchPet pet || __result == null)
        {
            return;
        }

        // "Visuals" is a direct child of the host root (rocket.tscn and fallback.tscn both have it).
        Sprite2D? sprite = __result.GetNodeOrNull<Sprite2D>("Visuals");
        if (sprite == null || !ResourceLoader.Exists(pet.TexturePath))
        {
            return;
        }

        sprite.Texture = PreloadManager.Cache.GetTexture2D(pet.TexturePath);
        sprite.Visible = true;
        sprite.Scale = Vector2.One * pet.SpriteScale;
        sprite.Position = pet.SpriteOffset;
    }
}
