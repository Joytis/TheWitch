using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// PROTOTYPE. When the GDCubism extension is installed, swaps the Witch's static body sprite for a Live2D model
/// right after her visuals scene is instantiated (before _Ready), same shape as
/// <see cref="Monsters.WitchPetVisualsPatch"/>. Covers every CreateVisuals caller: combat, game
/// over, unlock screen, fake merchant. Any failure leaves the static sprite in place.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
public static class WitchLive2DVisualsPatch
{
    // Roughly the static sprite's height (witch_visuals.tscn Bounds is 270 tall).
    private const float ModelHeight = 300f;

    private static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not Witch || __result == null || !Live2DRuntime.EnsureLoaded())
        {
            return;
        }

        string? modelJson = Live2DRuntime.FindModelJson();
        if (modelJson == null)
        {
            return;
        }

        Sprite2D body = __result.GetNode<Sprite2D>("VisualsRoot/Visuals");
        AnimationTree? tree = __result.GetNodeOrNull<AnimationTree>("AnimationTree");
        NWitchLive2D.Attach(body, tree, modelJson, ModelHeight);
    }
}
