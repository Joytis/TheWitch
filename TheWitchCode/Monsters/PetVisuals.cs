using Godot;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Node script on the root of the pet scene (witch_pet_scene.tscn), instantiated by
/// <see cref="WitchPetVisualsPatch" /> and populated from a <see cref="PetConfig" />.
///
/// Populate is called right after Instantiate, BEFORE the node enters the tree — _Ready
/// has not run yet, so child nodes are resolved here, not cached in _Ready.
/// </summary>
[GlobalClass]
public partial class PetVisuals : Node2D
{
    private AnimationPlayer? _animationPlayer;
    private WitchPet? _pet;
    private Node2D? _visualsRoot;
    private Node2D? _shadow;

    // Static-event lifecycle: subscribe/unsubscribe must mirror tree membership, or the
    // event ends up holding a delegate to a freed node (ObjectDisposedException on fire).
    public override void _EnterTree() => FamiliarPower.AnimationRequested += OnAnimationRequested;
    public override void _ExitTree() => FamiliarPower.AnimationRequested -= OnAnimationRequested;

    private void OnAnimationRequested(FamiliarPower power, int stackIndex, string animation)
    {
        if (_pet != null && ReferenceEquals(_pet.SourcePower, power) && _pet.StackIndex == stackIndex)
        {
            PlayAnimation(animation);
        }
    }

    public override void _Ready()
    {
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_animationPlayer != null)
        {
            // Non-looping one-shots (attack/skill) fall back to idle when they finish.
            _animationPlayer.AnimationFinished += _ => PlayAnimation("idle");

            // Slight per-pet playback-speed variance so a crowd of pets doesn't bob in unison.
            // Cosmetic + local-only, so plain engine randomness is fine (no game RNG involved).
            _animationPlayer.SpeedScale = 0.95f + GD.Randf() * 0.1f;
        }

        _visualsRoot = GetNodeOrNull<Node2D>("VisualsRoot");
        _shadow = GetNodeOrNull<Node2D>("Shadow");

        PlayAnimation("idle");
    }

    // The shadow is a sibling of VisualsRoot, so it inherits none of the animation's
    // scale/rotation/hop — it only tracks the lunge's horizontal movement.
    public override void _Process(double delta)
    {
        if (_shadow != null && _visualsRoot != null)
        {
            _shadow.Position = new Vector2(_visualsRoot.Position.X, _shadow.Position.Y);
        }
    }

    /// <summary>Play a named animation ("idle" / "attack" / "skill") if the scene's AnimationPlayer has it.</summary>
    public void PlayAnimation(string name)
    {
        if (_animationPlayer != null && _animationPlayer.HasAnimation(name))
        {
            _animationPlayer.Play(name);
        }
    }

    public void Populate(PetConfig config, WitchPet pet)
    {
        _pet = pet;
        Sprite2D sprite = GetNode<Sprite2D>("VisualsRoot/Visuals");
        sprite.Texture = config.Texture;

        // Pivot at the sprite's bottom-center: with Centered on, shifting the draw rect up by
        // half the texture height puts the node origin at the feet — simpler animations.
        if (config.Texture is { } tex)
        {
            sprite.Offset = new Vector2(0f, -tex.GetHeight() / 2f);
        }

        // Scale the sprite only — VisualsRoot belongs to the animations.
        sprite.Scale = Vector2.One * config.VisualsScale;

        if (GetNodeOrNull<Node2D>("Shadow") is { } shadow)
        {
            shadow.Visible = config.HasShadow;
        }

        Position = config.Offset;
    }
}
