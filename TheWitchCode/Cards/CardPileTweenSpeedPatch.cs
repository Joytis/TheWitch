using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Scoped speed-up for the game's card pile-move animation. While <see cref="Scale" /> is above 1,
/// every pile-move tween built by <c>CardPileCmd.AppendPileLerpTween</c> plays at that speed
/// (Godot <see cref="Tween.SetSpeedScale" /> covers the whole chain — lerp, interval, fade).
/// The game hard-codes those durations, so this is the only way to run them faster while keeping
/// the animation. Purely visual + local; set it in a try/finally scope (see Mulch) so it can
/// never leak past one effect.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), "AppendPileLerpTween")]
public static class CardPileTweenSpeedPatch
{
    /// <summary>Speed multiplier for pile-move tweens. 1 = untouched.</summary>
    public static float Scale = 1f;

    private static void Postfix(Tween __0)
    {
        if (Scale > 1f)
        {
            __0.SetSpeedScale(Scale);
        }
    }
}
