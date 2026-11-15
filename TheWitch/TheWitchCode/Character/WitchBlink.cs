using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Random;
using Timer = Godot.Timer;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Makes the Witch blink. The game has no blink event, so a timer beside her Spine body plays the
/// rig's <see cref="BlinkAnimation"/> on Spine track 1 at random intervals; it layers over whatever
/// the game's animator plays on track 0 (idle, attack, hurt, ...). The blink animation must key
/// only the eye slots, or it will override the body pose while it plays.
/// Attached from <see cref="Witch.GenerateAnimator"/>.
/// </summary>
internal static class WitchBlink
{
    private const string BlinkAnimation = "blink";
    private const string DeathAnimation = "die";
    private const int BlinkTrack = 1;

    // Seconds between blinks, rolled per blink.
    private const float MinInterval = 2f;
    private const float MaxInterval = 10f;

    public static void Attach(MegaSprite sprite)
    {
        if (sprite.BoundObject is not Node2D body || body.GetParent() is not { } parent || !sprite.HasAnimation(BlinkAnimation))
        {
            return;
        }

        // Sibling of the SpineSprite (which owns its own children); freed with the visuals scene.
        Timer timer = new() { Name = "BlinkTimer", OneShot = true, Autostart = true, WaitTime = NextInterval() };
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(body))
            {
                Blink(body);
            }

            timer.Start(NextInterval());
        };
        parent.AddChild(timer);
    }

    private static float NextInterval() => Rng.Chaotic.NextFloat(MinInterval, MaxInterval);

    private static void Blink(Node2D body)
    {
        MegaAnimationState state = new MegaSprite(body).GetAnimationState();
        if (state.GetCurrentAnimationName() == DeathAnimation)
        {
            return;
        }

        state.SetAnimation(BlinkAnimation, loop: false, BlinkTrack);
        // Release the track once the blink ends, so its last frame stops overriding track 0.
        state.AddEmptyAnimation(BlinkTrack);
    }
}
