using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Hijacks creature-visual creation for <see cref="WitchPet" />s: the pet reuses the base
/// game's textureless "rocket" host scene (a valid NCreatureVisuals), and this postfix
/// injects our own pet scene under its %Visuals body, populated from the pet's PetConfig
/// .tres. Falls back to the original plain-PNG swap if the config or scene is missing.
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
        if (sprite == null)
        {
            return;
        }

        sprite.Visible = true;
        sprite.Scale = Vector2.One * pet.SpriteScale;

        if (TryInjectScene(pet, sprite))
        {
            return;
        }

        // Legacy fallback: swap the PNG straight onto the host body.
        if (ResourceLoader.Exists(pet.TexturePath))
        {
            sprite.Texture = PreloadManager.Cache.GetTexture2D(pet.TexturePath);
            sprite.Position = pet.SpriteOffset;
        }
    }

    /// <summary>
    /// Instantiates the pet scene under the host body and populates it from the pet's
    /// PetConfig. The host sprite stays textureless — it is purely the injection point;
    /// the config's Offset positions the scene, so the legacy SpriteOffset is not applied.
    /// </summary>
    private static bool TryInjectScene(WitchPet pet, Sprite2D sprite)
    {
        try
        {
            if (!ResourceLoader.Exists(pet.ConfigPath) || !ResourceLoader.Exists(pet.PetScenePath))
            {
                return false;
            }

            // Plain Load + pattern cast: GD.Load<T> THROWS InvalidCastException when the .tres script
            // didn't bind (unregistered assembly), and this runs inside a card-play action.
            if (ResourceLoader.Load(pet.ConfigPath) is not PetConfig cfg ||
                GD.Load<PackedScene>(pet.PetScenePath).Instantiate() is not PetVisuals visuals)
            {
                MainFile.Logger.Warn($"Pet scene/config for '{pet.PetFileName}' didn't bind mod scripts; using PNG fallback.");
                return false;
            }

            sprite.Texture = null;
            // The rocket host's Visuals sprite sits at an upward offset (body center) inside the
            // creature — zero it so the pet scene's origin lands on the creature origin (the feet),
            // otherwise every pet floats a body-height above its spawn marker.
            sprite.Position = Vector2.Zero;
            sprite.AddChild(visuals);
            visuals.Populate(cfg, pet);
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Pet scene injection failed for '{pet.PetFileName}': {e.Message}");
            return false;
        }
    }
}
