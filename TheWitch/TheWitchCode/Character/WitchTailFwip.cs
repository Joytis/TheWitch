using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Random;
using Timer = Godot.Timer;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Flicks the Witch's tail while she idles. Same shape as <see cref="WitchBlink"/>: a timer beside
/// her Spine body plays the rig's <see cref="FwipAnimation"/> on its own Spine track at random
/// intervals, layered over track 0. Unlike the blink it only fires while track 0 is on the idle
/// loop, so it never fights an attack / cast / hurt pose. The fwip animation must key only the
/// tail bones, or it will override the body pose while it plays.
/// Attached from <see cref="Witch.GenerateAnimator"/>.
/// </summary>
internal static class WitchTailFwip
{
    private const string FwipAnimation = "tail_fwip";
    private const string IdleAnimation = "idle_loop";
    // Track 1 is the blink.
    private const int FwipTrack = 2;

    // Seconds between fwips, rolled per fwip.
    private const float MinInterval = 3f;
    private const float MaxInterval = 10f;

    public static void Attach(MegaSprite sprite)
    {
        if (sprite.BoundObject is not Node2D body || body.GetParent() is not { } parent || !sprite.HasAnimation(FwipAnimation))
        {
            return;
        }

        // Sibling of the SpineSprite (which owns its own children); freed with the visuals scene.
        Timer timer = new() { Name = "TailFwipTimer", OneShot = true, Autostart = true, WaitTime = NextInterval() };
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(body))
            {
                Fwip(body);
            }

            timer.Start(NextInterval());
        };
        parent.AddChild(timer);
    }

    private static float NextInterval() => Rng.Chaotic.NextFloat(MinInterval, MaxInterval);

    private static void Fwip(Node2D body)
    {
        MegaAnimationState state = new MegaSprite(body).GetAnimationState();
        if (state.GetCurrentAnimationName() != IdleAnimation)
        {
            return;
        }

        state.SetAnimation(FwipAnimation, loop: false, FwipTrack);
        // Release the track once the fwip ends, so its last frame stops overriding track 0.
        state.AddEmptyAnimation(FwipTrack);
    }
}
