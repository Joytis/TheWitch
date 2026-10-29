using Godot;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// PROTOTYPE. Drives a GDCubism Live2D model from the Witch's existing visuals scene.
///
/// The witch_visuals.tscn AnimationTree stays the source of truth: BaseLib travels its state
/// machine (Idle / Attack / AttackSilent / Hit / PowerUp / Dead) and its tracks keep moving
/// VisualsRoot (lunge, recoil, death tumble), which the model inherits as a child of the body
/// sprite. This node watches the state machine and mirrors each state entry onto a Live2D
/// motion group of the same name, so a Witch model only has to author motion groups named after
/// the states. Between one-shots it plays a random "Idle" motion.
/// </summary>
public partial class NWitchLive2D : Node
{
    // Live2D sample models (Hiyori etc.) only ship "Idle" + "TapBody"; stand-ins until the
    // Witch model authors her own groups.
    private static readonly Dictionary<string, string[]> Fallbacks = new()
    {
        ["AttackSilent"] = ["Attack", "TapBody"],
        ["Attack"] = ["TapBody"],
        ["Hit"] = ["TapBody"],
        ["PowerUp"] = ["TapBody"],
    };

    // GDCubismUserModel.Priority
    private const int PriorityIdle = 1;
    private const int PriorityForce = 3;

    private const string IdleGroup = "Idle";
    private const string DeadState = "Dead";

    private Node2D _model = null!;
    private AnimationTree? _tree;
    private AnimationNodeStateMachinePlayback? _playback;
    private Godot.Collections.Dictionary _motionCounts = new();
    private StringName _lastState = new("");
    private double _lastPosition;
    private bool _dead;

    /// <summary>
    /// Builds the model for <paramref name="modelJson"/> and parents it under <paramref name="body"/>
    /// (the %Visuals sprite, whose own texture is hidden) with its feet on the creature origin and
    /// <paramref name="height"/> px tall. Null when the extension or model fails to load.
    /// </summary>
    public static Node2D? Attach(Sprite2D body, AnimationTree? tree, string modelJson, float height)
    {
        if (ClassDB.Instantiate(Live2DRuntime.ModelClass).AsGodotObject() is not Node2D model)
        {
            MainFile.Logger.Error($"[live2d] could not instantiate {Live2DRuntime.ModelClass}");
            return null;
        }

        model.Name = "Live2DModel";
        model.Set("assets", modelJson);
        Godot.Collections.Dictionary canvas = model.Call("get_canvas_info").AsGodotDictionary();
        if (!canvas.ContainsKey("size_in_pixels"))
        {
            MainFile.Logger.Error($"[live2d] model failed to load: {modelJson}");
            model.Free();
            return null;
        }

        // The model draws centred on its canvas origin; scale to the target height and lift it so
        // the bottom of the canvas sits on the creature origin. Divide out the body sprite's own
        // scale since the model is its child.
        Vector2 size = canvas["size_in_pixels"].AsVector2();
        Vector2 origin = canvas["origin_in_pixels"].AsVector2();
        float k = height / size.Y;
        model.Scale = Vector2.One * k / body.Scale;
        model.Position = new Vector2(0f, -(size.Y - origin.Y) * k) / body.Scale;

        // Blink + breathing come from GDCubism effect nodes reading the model's standard params.
        foreach (string effect in new[] { "GDCubismEffectEyeBlink", "GDCubismEffectBreath" })
        {
            if (ClassDB.Instantiate(effect).AsGodotObject() is Node node)
            {
                model.AddChild(node);
            }
        }

        model.AddChild(new NWitchLive2D { Name = "Driver", _model = model, _tree = tree });
        body.AddChild(model);
        // Keep the body sprite in the tree (NCreatureVisuals uses it as %Visuals and its
        // modulate/visibility should still reach the model); just stop it drawing its texture.
        body.SelfModulate = Colors.Transparent;
        return model;
    }

    public override void _Ready()
    {
        _motionCounts = _model.Call("get_motions").AsGodotDictionary();
        _playback = _tree?.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        // Fired from inside the Cubism motion update; defer so the new motion isn't queued mid-update.
        _model.Connect("motion_finished", Callable.From(() => Callable.From(PlayIdle).CallDeferred()));
        MainFile.Logger.Info($"[live2d] model ready, motion groups: {_motionCounts}");
        PlayIdle();
    }

    public override void _Process(double delta)
    {
        if (_playback == null)
        {
            return;
        }

        StringName state = _playback.GetCurrentNode();
        double position = _playback.GetCurrentPlayPosition();
        // BaseLib travels with reset, so re-triggering the current state (two attacks in a row)
        // shows up as the play position jumping back.
        bool restarted = state == _lastState && position + 0.0001 < _lastPosition;
        if (state != _lastState || restarted)
        {
            OnStateEntered(state.ToString());
        }
        _lastState = state;
        _lastPosition = position;
    }

    private void OnStateEntered(string state)
    {
        bool wasDead = _dead;
        _dead = state == DeadState;
        if (state == IdleGroup)
        {
            // Otherwise let the current one-shot finish; motion_finished brings the idle back.
            if (wasDead)
            {
                PlayIdle();
            }
            return;
        }

        string? group = new[] { state }.Concat(Fallbacks.GetValueOrDefault(state, [])).FirstOrDefault(HasGroup);
        MainFile.Logger.Info($"[live2d] state {state} -> motion group {group ?? "(none)"}");
        if (group != null)
        {
            _model.Call("start_motion", group, RandomMotion(group), PriorityForce);
        }
    }

    private void PlayIdle()
    {
        if (_dead || !HasGroup(IdleGroup))
        {
            return;
        }
        // IDLE priority is refused while a one-shot is still playing, which is what we want.
        _model.Call("start_motion", IdleGroup, RandomMotion(IdleGroup), PriorityIdle);
    }

    // Cosmetic only, so Godot's global RNG rather than a synced run RNG.
    private int RandomMotion(string group) => (int)(GD.Randi() % (uint)MotionCount(group));

    private bool HasGroup(string group) => MotionCount(group) > 0;

    private int MotionCount(string group) =>
        _motionCounts.TryGetValue(group, out Variant count) ? count.AsInt32() : 0;
}
