using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Bird.BirdDebug;

/// <summary>
/// Frame-time probe for the selected character's combat body (--bird-perf[=SECONDS]): starts a fixed
/// run (seed BIRDPERF, encounter CULTISTS_NORMAL), waits for the play phase, uncaps the frame rate
/// (vsync off, Engine.MaxFps = 0, re-asserted every frame so background throttling can't cap it) and
/// samples three phases of SECONDS each (default 10, first second of each discarded):
///   baseline   normal combat
///   frozen     the player's SpineSprite stops processing (no animation / physics / mesh rebuild;
///              still drawn) - baseline minus frozen = what the body's Spine update costs per frame
///   copies     N extra SpineSprites with the player's skeleton playing idle_loop (--bird-perf-copies,
///              default 8), on screen - (copies - baseline) / N = cost of one more instance
///   update     the same copies switched to manual update; each one's update_skeleton(1/60) is timed
///              with a Stopwatch every frame = exact CPU cost of one Spine update at a 60 fps step.
///              This is the number that matters for physics: Spine physics constraints step at a fixed
///              rate (1/60 s by default), so at an uncapped 600+ fps most frames skip the physics step
///              and the frame-time phases UNDER-count it; at 60 fps every frame pays it.
/// Per frame: frame time (between process frames), viewport render CPU/GPU time, draw calls. Also logs
/// the skeleton's bone / slot / constraint counts. Writes JSON to
/// --bird-perf-out and quits (0 = ok, 1 = failed). Any character works: -bird-character=Ironclad too.
/// Driven by tools/perf-spine.ps1. Tag [bird-perf].
/// </summary>
public static class PerfProbe
{
    public const string Tag = "[bird-perf]";
    private const string Encounter = "CULTISTS_NORMAL";
    private static bool _started;

    public static bool TryApply(Harmony harmony)
    {
        if (!CommandLineHelper.HasArg("bird-perf"))
        {
            return false;
        }
        BirdDebug.Logger.Info($"{Tag} will profile {BirdCharacter.Key} in combat");
        harmony.Patch(AccessTools.Method(typeof(NMainMenu), "_Ready"),
            postfix: new HarmonyMethod(typeof(PerfProbe), nameof(MainMenuReadyPostfix)));
        return true;
    }

    private static void MainMenuReadyPostfix()
    {
        if (_started)
        {
            return;
        }
        _started = true;
        TaskHelper.RunSafely(RunAndQuit());
    }

    private static async Task RunAndQuit()
    {
        int code = 0;
        try
        {
            await Run();
        }
        catch (Exception e)
        {
            BirdDebug.Logger.Error($"{Tag} FAILED: {e}");
            code = 1;
        }
        Game.GetTree().Quit(code);
    }

    private static async Task Run()
    {
        double seconds = ParseDouble(CommandLineHelper.GetValue("bird-perf"), 10);
        int copies = (int)ParseDouble(CommandLineHelper.GetValue("bird-perf-copies"), 8);
        string? outPath = CommandLineHelper.GetValue("bird-perf-out")?.Trim('"');

        NAudioManager.Instance?.SetMasterVol(0f);
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
        await Frames(2);

        await Game.StartNewSingleplayerRun(BirdCharacter.Model, shouldSave: false, ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(), "BIRDPERF", GameMode.Standard);
        EncounterModel encounter = ModelDb.GetById<EncounterModel>(new ModelId(ModelId.SlugifyCategory<EncounterModel>(), Encounter)).ToMutable();
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Unassigned, encounter);

        Player player = await WaitForPlayPhase();
        await Seconds(3);   // turn banner, opening draw, intro tweens

        NCreature creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature)
                             ?? throw new InvalidOperationException("no player creature node");
        // %Visuals = the body node NCreatureVisuals wraps (public v0.107 has no Body accessor).
        Node2D body = creature.Visuals.GetNode<Node2D>("%Visuals");
        if (body.GetClass() != "SpineSprite")
        {
            throw new InvalidOperationException($"{BirdCharacter.Key}'s combat body is a {body.GetClass()}, not a SpineSprite");
        }
        Dictionary<string, int> skeleton = SkeletonStats(body);
        BirdDebug.Logger.Info($"{Tag} {BirdCharacter.Key} skeleton: {string.Join(", ", skeleton.Select(kv => $"{kv.Key}={kv.Value}"))}");

        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Rid viewport = Game.GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(viewport, true);

        PhaseStats baseline = await Measure("baseline", seconds, viewport);

        Node.ProcessModeEnum mode = body.ProcessMode;
        body.ProcessMode = Node.ProcessModeEnum.Disabled;
        PhaseStats frozen = await Measure("frozen", seconds, viewport);
        body.ProcessMode = mode;

        List<Node2D> extra = SpawnCopies(body, copies);
        await Frames(5);
        foreach (Node2D copy in extra)
        {
            new MegaSprite(copy).GetAnimationState().SetAnimation("idle_loop");
        }
        PhaseStats withCopies = await Measure($"copies x{copies}", seconds, viewport);
        // Where the update time goes: the same copies with the animation cleared (bones hold their last
        // pose; physics + mesh still run), then physics constraints muted too (mix 0: PhysicsConstraint
        // returns early), then the animation back on with physics still muted.
        UpdateStats update = await MeasureManualUpdate(extra, seconds, "full");
        ForEachCall(extra, c => c.Call("get_animation_state").AsGodotObject().Call("clear_tracks"));
        UpdateStats noAnim = await MeasureManualUpdate(extra, seconds, "no animation");
        ForEachCall(extra, c => SetPhysicsMix(c, 0));
        UpdateStats bare = await MeasureManualUpdate(extra, seconds, "no animation, no physics");
        foreach (Node2D copy in extra)
        {
            new MegaSprite(copy).GetAnimationState().SetAnimation("idle_loop");
        }
        UpdateStats noPhysics = await MeasureManualUpdate(extra, seconds, "no physics");
        foreach (Node2D copy in extra)
        {
            copy.QueueFree();
        }

        var result = new
        {
            character = BirdCharacter.Key,
            seconds,
            copies,
            skeleton,
            window = DisplayServer.WindowGetSize().ToString(),
            gpu = RenderingServer.GetVideoAdapterName(),
            phases = new[] { baseline, frozen, withCopies },
            update,
            updateBreakdown = new[] { update, noAnim, noPhysics, bare },
            bodyFrameMs = baseline.FrameMsMean - frozen.FrameMsMean,
            perCopyFrameMs = (withCopies.FrameMsMean - baseline.FrameMsMean) / Math.Max(1, copies),
            perCopyGpuMs = (withCopies.GpuMsMean - baseline.GpuMsMean) / Math.Max(1, copies),
            perCopyDrawCalls = (withCopies.DrawCallsMean - baseline.DrawCallsMean) / Math.Max(1, copies),
        };
        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
        BirdDebug.Logger.Info($"{Tag} RESULT {json.ReplaceLineEndings(" ")}");
        if (!string.IsNullOrWhiteSpace(outPath))
        {
            System.IO.File.WriteAllText(outPath, json);
            BirdDebug.Logger.Info($"{Tag} wrote {outPath}");
        }
    }

    private static async Task<Player> WaitForPlayPhase()
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            Player? player = CombatManager.Instance.DebugOnlyGetState() is { } state ? LocalContext.GetMe(state) : null;
            if (player?.PlayerCombatState?.Phase == PlayerTurnPhase.Play)
            {
                return player;
            }
            await Frames(1);
        }
        throw new TimeoutException("combat never reached the play phase");
    }

    /// <summary>Extra instances of the body's skeleton, on screen in two rows right of the player.</summary>
    private static List<Node2D> SpawnCopies(Node2D body, int count)
    {
        Variant data = body.Get("skeleton_data_res");
        Node parent = NCombatRoom.Instance!;
        List<Node2D> spawned = new();
        for (int i = 0; i < count; i++)
        {
            Node2D copy = (Node2D)ClassDB.Instantiate("SpineSprite").AsGodotObject();
            parent.AddChild(copy);
            copy.Set("skeleton_data_res", data);
            copy.GlobalScale = body.GlobalScale;
            copy.GlobalPosition = body.GlobalPosition + new Vector2(260 + (i % 4) * 230, i / 4 * -360);
            spawned.Add(copy);
        }
        return spawned;
    }

    private static Dictionary<string, int> SkeletonStats(Node2D body)
    {
        Dictionary<string, int> stats = new();
        try
        {
            GodotObject data = body.Call("get_skeleton").AsGodotObject().Call("get_data").AsGodotObject();
            foreach ((string key, string method) in new[]
                     {
                         ("bones", "get_bones"), ("slots", "get_slots"), ("animations", "get_animations"),
                         ("ikConstraints", "get_ik_constraints"), ("transformConstraints", "get_transform_constraints"),
                         ("pathConstraints", "get_path_constraints"), ("physicsConstraints", "get_physics_constraints"),
                     })
            {
                if (data.HasMethod(method))
                {
                    stats[key] = data.Call(method).AsGodotArray().Count;
                }
            }
        }
        catch (Exception e)
        {
            BirdDebug.Logger.Warn($"{Tag} skeleton stats unavailable: {e.Message}");
        }
        return stats;
    }

    private static async Task<PhaseStats> Measure(string name, double seconds, Rid viewport)
    {
        List<double> frame = new(), gpu = new(), renderCpu = new(), draws = new();
        SceneTree tree = Game.GetTree();
        Stopwatch total = Stopwatch.StartNew();
        Stopwatch tick = Stopwatch.StartNew();
        while (total.Elapsed.TotalSeconds < seconds)
        {
            Engine.MaxFps = 0;
            await Game.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            double ms = tick.Elapsed.TotalMilliseconds;
            tick.Restart();
            if (total.Elapsed.TotalSeconds < 1)
            {
                continue;   // settle after the phase switch
            }
            frame.Add(ms);
            gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport));
            renderCpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport));
            draws.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
        }
        PhaseStats stats = new()
        {
            Name = name,
            Frames = frame.Count,
            Fps = frame.Count / frame.Sum() * 1000,
            FrameMsMean = frame.Average(),
            FrameMsP50 = Percentile(frame, 50),
            FrameMsP95 = Percentile(frame, 95),
            FrameMsP99 = Percentile(frame, 99),
            FrameMsMax = frame.Max(),
            RenderCpuMsMean = renderCpu.Average(),
            GpuMsMean = gpu.Average(),
            DrawCallsMean = draws.Average(),
        };
        BirdDebug.Logger.Info(string.Create(CultureInfo.InvariantCulture,
            $"{Tag} {BirdCharacter.Key} {name}: {stats.Fps:F0} fps, frame {stats.FrameMsMean:F2} ms (p95 {stats.FrameMsP95:F2}, p99 {stats.FrameMsP99:F2}), render cpu {stats.RenderCpuMsMean:F2} ms, gpu {stats.GpuMsMean:F2} ms, {stats.DrawCallsMean:F0} draws"));
        return stats;
    }

    private static void ForEachCall(List<Node2D> copies, Action<Node2D> action)
    {
        foreach (Node2D copy in copies)
        {
            action(copy);
        }
    }

    private static void SetPhysicsMix(Node2D sprite, float mix)
    {
        GodotObject skeleton = sprite.Call("get_skeleton").AsGodotObject();
        foreach (Variant constraint in skeleton.Call("get_physics_constraints").AsGodotArray())
        {
            constraint.AsGodotObject().Call("set_mix", mix);
        }
    }

    private static double Percentile(List<double> values, double p)
    {
        List<double> sorted = values.OrderBy(v => v).ToList();
        return sorted[Math.Clamp((int)Math.Ceiling(p / 100 * sorted.Count) - 1, 0, sorted.Count - 1)];
    }

    private static double ParseDouble(string? s, double fallback) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0 ? v : fallback;

    private static NGame Game => NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null");

    private static async Task Frames(int n)
    {
        SceneTree tree = Game.GetTree();
        for (int i = 0; i < n; i++)
        {
            await Game.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task Seconds(double s) =>
        await Game.ToSignal(Game.GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout);

    private sealed class PhaseStats
    {
        public string Name = "";
        public int Frames;
        public double Fps, FrameMsMean, FrameMsP50, FrameMsP95, FrameMsP99, FrameMsMax;
        public double RenderCpuMsMean, GpuMsMean, DrawCallsMean;
    }

    private sealed class UpdateStats
    {
        public string Name = "";
        public int Samples;
        public double UsMean, UsP50, UsP95, UsP99, UsMax;
    }

    /// <summary>
    /// Times SpineSprite.update_skeleton(1/60) per copy (update_mode = Manual): animation apply, physics
    /// step, world transforms and mesh rebuild - the whole per-frame Spine CPU cost of one instance at 60 fps.
    /// </summary>
    private static async Task<UpdateStats> MeasureManualUpdate(List<Node2D> copies, double seconds, string name)
    {
        const int manual = 2;   // SpineConstant.UpdateMode_Manual
        foreach (Node2D copy in copies)
        {
            copy.Set("update_mode", manual);
        }
        List<double> us = new();
        SceneTree tree = Game.GetTree();
        Stopwatch total = Stopwatch.StartNew();
        Stopwatch one = new();
        while (total.Elapsed.TotalSeconds < seconds)
        {
            await Game.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            bool settle = total.Elapsed.TotalSeconds < 1;
            foreach (Node2D copy in copies)
            {
                one.Restart();
                copy.Call("update_skeleton", 1.0 / 60.0);
                one.Stop();
                if (!settle)
                {
                    us.Add(one.Elapsed.TotalMilliseconds * 1000);
                }
            }
        }
        UpdateStats stats = new()
        {
            Name = name,
            Samples = us.Count,
            UsMean = us.Average(),
            UsP50 = Percentile(us, 50),
            UsP95 = Percentile(us, 95),
            UsP99 = Percentile(us, 99),
            UsMax = us.Max(),
        };
        BirdDebug.Logger.Info(string.Create(CultureInfo.InvariantCulture,
            $"{Tag} {BirdCharacter.Key} update_skeleton(1/60) [{name}]: {stats.UsMean:F0} us mean (p50 {stats.UsP50:F0}, p95 {stats.UsP95:F0}, p99 {stats.UsP99:F0}, max {stats.UsMax:F0}) over {stats.Samples} updates"));
        return stats;
    }
}
