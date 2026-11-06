using Godot;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// PROTOTYPE. Drives a GDCubism Live2D model mounted under a scene's <c>Visuals</c> node
/// (see <see cref="WitchLive2DMount"/>).
///
/// In combat the witch_visuals.tscn AnimationTree stays the source of truth: BaseLib travels its
/// state machine (Idle / Attack / AttackSilent / Hit / PowerUp / Dead) and its tracks keep moving
/// VisualsRoot (lunge, recoil, death tumble), which the model inherits through Visuals. This
/// node watches the state machine and mirrors each state entry onto a Live2D motion group of the
/// same name, so a Witch model only has to author motion groups named after the states. Between
/// one-shots, and in scenes without a tree (rest site, merchant), it plays a random "Idle" motion.
/// </summary>
public partial class NWitchLive2D : Node
{
    // Motion groups tried, in order, when the model has no group named after the state.
    private static readonly Dictionary<string, string[]> Fallbacks = new()
    {
        ["AttackSilent"] = ["Attack"],
    };

    // WitchCubismUserModel.Priority
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
    private bool _partsLogged;

    /// <summary>
    /// Builds the model for <paramref name="modelJson"/> under <paramref name="root"/> at its native
    /// canvas size, bottom-centre of the canvas on the root's origin, so the root's own transform is
    /// the only placement knob. <paramref name="breathIntensity"/> scales the breathing effect's
    /// swings (1 = GDCubism's stock curves). Null when the extension or model fails to load.
    /// </summary>
    public static Node2D? Attach(Node2D root, AnimationTree? tree, string modelJson, float breathIntensity = 1f)
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

        // The model draws centred on its canvas origin; lift it so the bottom of the canvas sits on
        // the root's origin.
        Vector2 size = canvas["size_in_pixels"].AsVector2();
        Vector2 origin = canvas["origin_in_pixels"].AsVector2();
        model.Position = new Vector2(0f, -(size.Y - origin.Y));
        MainFile.Logger.Info($"[live2d] {modelJson}: canvas {size}, origin {origin}, mounted under {root.Name}");

        // Blink + breathing come from GDCubism effect nodes reading the model's standard params.
        // PLACEHOLDER: EffectBreath sways ParamAngleX/Y/Z, ParamBodyAngleX and ParamBreath on stock
        // sine curves (gd_cubism_effect_breath.hpp), on top of whatever motion plays. It stands in
        // for the artist's Idle motions; drop it (or set intensity 0) once those arrive, or it will
        // layer extra head sway over them. `intensity` is a Witch-fork property (fork rev 4); an
        // older plugin build ignores it and breathes at 1.
        foreach (string effect in new[] { "EffectEyeBlink", "EffectBreath" })
        {
            if (ClassDB.Instantiate(Live2DRuntime.ClassPrefix + effect).AsGodotObject() is Node node)
            {
                if (effect == "EffectBreath")
                {
                    node.Set("intensity", breathIntensity);
                    MainFile.Logger.Info($"[live2d] placeholder breathing on, intensity {node.Get("intensity")}");
                }
                model.AddChild(node);
            }
        }

        model.AddChild(new NWitchLive2D { Name = "Driver", _model = model, _tree = tree });
        root.AddChild(model);
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
        if (!_partsLogged)
        {
            // Parts are built on the model's first update, after this node's _Ready.
            _partsLogged = true;
            Live2DBlendOverrides.Apply(_model, _model.Get("assets").AsString().GetBaseDir().GetFile());
            LogParts();
        }

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

    /// <summary>
    /// One line per model: how many parts use each blend shader (2d_cubism_norm_mix = Normal,
    /// _mul = Multiply, _add = Additive; mask_* = clipped by a mask). Tells you whether a part the
    /// artist meant as Multiply was actually exported as one.
    /// </summary>
    private void LogParts()
    {
        var counts = new SortedDictionary<string, int>();
        foreach (MeshInstance2D part in _model.FindChildren("*", nameof(MeshInstance2D), recursive: false, owned: false).OfType<MeshInstance2D>())
        {
            string shader = (part.Material as ShaderMaterial)?.Shader?.ResourcePath.GetFile().GetBaseName() ?? "(none)";
            counts[shader] = counts.GetValueOrDefault(shader) + 1;
        }
        MainFile.Logger.Info($"[live2d] {_model.Get("assets")}: parts by blend shader: {string.Join(", ", counts.Select(c => $"{c.Key} {c.Value}"))}");
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
