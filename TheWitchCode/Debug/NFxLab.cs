using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Debug-only FX browser scene (launched via -witch-debug -witch-fxlab, see WitchDebug).
/// Left list: every FMOD sfx event (parsed out of the shipped *.strings.bank) plus the
/// res://debug_audio mp3s. Right list: every vfx particle scene (res://scenes/vfx plus any
/// mod vfx scenes under res://TheWitch). Each row: Play, and Copy (puts the exact string the
/// code APIs take into the Windows clipboard). Vfx spawn on the stage panel and free
/// themselves; a failsafe frees anything still alive after a few seconds.
/// Built entirely in code so a plain `dotnet build` ships it (no .pck export needed).
/// </summary>
public partial class NFxLab : Control
{
    private const double VfxFailsafeSeconds = 8.0;
    private const double LoopVfxKillSeconds = 3.0;

    // Spawned vfx are scaled down so big effects fit inside the stage panel.
    private const float StageZoom = 0.5f;

    private sealed record FxEntry(string Display, string CopyText, string PlayKey, double MaxLifetime = VfxFailsafeSeconds, string Tag = "");

    private readonly List<(Control Row, string Filter)> _sfxRows = new();
    private readonly List<(Control Row, string Filter)> _vfxRows = new();

    private Label _sfxCount = null!;
    private Label _vfxCount = null!;
    private Control _stage = null!;

    public static NFxLab Create() => new() { Name = "FxLab" };

    public override void _Ready()
    {
        // Silence the main-menu music that is still playing when we replace the menu scene.
        NAudioManager.Instance?.StopMusic();

        // Size ourselves to the viewport directly instead of relying on parent anchors —
        // NSceneContainer doesn't force its children full-rect, so an anchored-only child
        // ends up with a stale rect.
        UpdateSize();
        GetViewport().SizeChanged += UpdateSize;

        ColorRect bg = new() { Color = new Color(0.09f, 0.09f, 0.12f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        VBoxContainer rootBox = new();
        rootBox.SetAnchorsPreset(LayoutPreset.FullRect);
        rootBox.OffsetLeft = 12;
        rootBox.OffsetTop = 8;
        rootBox.OffsetRight = -12;
        rootBox.OffsetBottom = -8;
        AddChild(rootBox);

        rootBox.AddChild(BuildToolbar());

        HBoxContainer columns = new();
        columns.SizeFlagsVertical = SizeFlags.ExpandFill;
        columns.AddThemeConstantOverride("separation", 12);
        rootBox.AddChild(columns);

        List<FxEntry> sfx = CollectSfxEntries();
        List<FxEntry> vfx = CollectVfxEntries();

        columns.AddChild(BuildListPanel($"SFX ({sfx.Count})", sfx, _sfxRows, PlaySfx, out _sfxCount));
        columns.AddChild(BuildListPanel($"VFX ({vfx.Count})", vfx, _vfxRows, PlayVfx, out _vfxCount, withOneShotToggle: true));
        columns.AddChild(BuildStage());

        MainFile.Logger.Info($"FX Lab ready: {sfx.Count} sfx, {vfx.Count} vfx");
    }

    private void UpdateSize()
    {
        Position = Vector2.Zero;
        Size = GetViewportRect().Size;
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= UpdateSize;
    }

    // ------------------------------------------------------------------ UI --

    private Control BuildToolbar()
    {
        HBoxContainer bar = new();
        bar.AddThemeConstantOverride("separation", 12);

        Label title = new() { Text = "Witch FX Lab" };
        title.AddThemeFontSizeOverride("font_size", 26);
        bar.AddChild(title);

        Control spacer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddChild(spacer);

        Button killVfx = new() { Text = "Kill VFX" };
        killVfx.Pressed += () =>
        {
            foreach (Node child in _stage.GetChildren())
            {
                child.QueueFree();
            }
        };
        bar.AddChild(killVfx);

        Button stop = new() { Text = "Stop Sounds" };
        stop.Pressed += () =>
        {
            NAudioManager.Instance?.StopAllLoops();
            NDebugAudioManager.Instance?.StopAll();
        };
        bar.AddChild(stop);

        Button quit = new() { Text = "Quit Game" };
        quit.Pressed += () => GetTree().Quit();
        bar.AddChild(quit);

        return bar;
    }

    private Control BuildListPanel(
        string header,
        List<FxEntry> entries,
        List<(Control Row, string Filter)> rowStore,
        Action<FxEntry> play,
        out Label countLabel,
        bool withOneShotToggle = false)
    {
        VBoxContainer panel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.8f, // lists 20% narrower than the stage's share
        };
        panel.CustomMinimumSize = new Vector2(344, 0);

        Label headerLabel = new() { Text = header };
        headerLabel.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(headerLabel);

        LineEdit search = new() { PlaceholderText = "filter...", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CheckBox? oneShotToggle = null;
        if (withOneShotToggle)
        {
            HBoxContainer searchRow = new();
            searchRow.AddChild(search);
            oneShotToggle = new CheckBox { Text = "one-shot only" };
            oneShotToggle.AddThemeFontSizeOverride("font_size", 12);
            searchRow.AddChild(oneShotToggle);
            panel.AddChild(searchRow);
        }
        else
        {
            panel.AddChild(search);
        }

        countLabel = new Label { Text = $"{entries.Count} shown" };
        countLabel.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(countLabel);

        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);

        VBoxContainer list = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(list);

        foreach (FxEntry entry in entries)
        {
            HBoxContainer row = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            Button playBtn = new() { Text = "Play", CustomMinimumSize = new Vector2(52, 0) };
            playBtn.Pressed += () => play(entry);
            row.AddChild(playBtn);

            Button copyBtn = new() { Text = "Copy", CustomMinimumSize = new Vector2(52, 0) };
            copyBtn.Pressed += () =>
            {
                DisplayServer.ClipboardSet(entry.CopyText);
                MainFile.Logger.Info($"FX Lab: copied '{entry.CopyText}'");
            };
            row.AddChild(copyBtn);

            Label pathLabel = new()
            {
                Text = entry.Display,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TooltipText = entry.CopyText,
                MouseFilter = MouseFilterEnum.Stop, // so the tooltip shows
            };
            pathLabel.AddThemeFontSizeOverride("font_size", 14);
            row.AddChild(pathLabel);

            if (entry.Tag.Length > 0)
            {
                Label tagLabel = new() { Text = entry.Tag };
                tagLabel.AddThemeFontSizeOverride("font_size", 12);
                tagLabel.AddThemeColorOverride("font_color", entry.Tag switch
                {
                    "[one-shot]" => new Color(0.45f, 0.9f, 0.45f),
                    "[loop]" => new Color(0.95f, 0.75f, 0.35f),
                    "[mp3]" => new Color(0.5f, 0.8f, 0.95f),
                    _ => new Color(0.6f, 0.6f, 0.7f),
                });
                row.AddChild(tagLabel);
            }

            list.AddChild(row);
            rowStore.Add((row, $"{entry.Display} {entry.Tag}".ToLowerInvariant()));
        }

        Label count = countLabel;
        CheckBox? toggle = oneShotToggle;
        void ApplyFilter()
        {
            string t = search.Text.Trim().ToLowerInvariant();
            bool oneShotOnly = toggle?.ButtonPressed ?? false;
            int shown = 0;
            foreach ((Control row, string filter) in rowStore)
            {
                bool visible = (t.Length == 0 || filter.Contains(t))
                               && (!oneShotOnly || filter.Contains("[one-shot]"));
                row.Visible = visible;
                if (visible)
                {
                    shown++;
                }
            }
            count.Text = $"{shown} shown";
        }
        search.TextChanged += _ => ApplyFilter();
        if (toggle != null)
        {
            toggle.Toggled += _ => ApplyFilter();
        }

        return panel;
    }

    private Control BuildStage()
    {
        PanelContainer stagePanel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(420, 0),
        };

        ColorRect stageBg = new() { Color = new Color(0.04f, 0.04f, 0.06f) };
        stagePanel.AddChild(stageBg);

        _stage = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true,
        };
        stagePanel.AddChild(_stage);

        return stagePanel;
    }

    // ------------------------------------------------------------ playback --

    private static void PlaySfx(FxEntry entry)
    {
        try
        {
            if (entry.PlayKey.StartsWith("event:/", StringComparison.Ordinal))
            {
                NAudioManager.Instance?.PlayOneShot(entry.PlayKey);
            }
            else
            {
                NDebugAudioManager.Instance?.Play(entry.PlayKey);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: sfx '{entry.PlayKey}' failed: {e}");
        }
    }

    private void PlayVfx(FxEntry entry)
    {
        try
        {
            PackedScene packed = ResourceLoader.Load<PackedScene>(entry.PlayKey);
            Node node = packed.Instantiate<Node>(PackedScene.GenEditState.Disabled);
            _stage.AddChild(node);
            Vector2 center = _stage.GlobalPosition + _stage.Size * 0.5f;
            // Many vfx scenes draw offset from their origin; recenter using their
            // visual bounds (particle visibility rects + sprite rects) when we can.
            // Roots are either Node2D or Control (e.g. hellraiser_sword_vfx) — the two
            // don't share a positioning API, hence the twin branches.
            if (node is Node2D node2D)
            {
                node2D.Scale *= StageZoom;
                node2D.GlobalPosition = center;
                Rect2? bounds = null;
                AccumulateBounds(node2D, Transform2D.Identity, ref bounds, isRoot: true);
                if (bounds.HasValue)
                {
                    node2D.GlobalPosition = center - node2D.GlobalTransform.BasisXform(bounds.Value.GetCenter());
                }
            }
            else if (node is Control control)
            {
                control.Scale *= StageZoom;
                control.GlobalPosition = center;
                Rect2? bounds = null;
                AccumulateBounds(control, Transform2D.Identity, ref bounds, isRoot: true);
                if (bounds.HasValue)
                {
                    control.GlobalPosition = center - control.GetGlobalTransform().BasisXform(bounds.Value.GetCenter());
                }
            }
            KickDormantParticles(node);
            _ = FreeAfterFailsafe(node, entry.MaxLifetime);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: vfx '{entry.PlayKey}' failed: {e}");
        }
    }

    // Building-block sub-scenes (e.g. vfx/common/vfx_common_hit_flare) ship with
    // emitting=false and rely on a parent scene/script calling restart(). If NOTHING in the
    // instanced scene is emitting, kick every emitter so the preview shows something. Scenes
    // where at least one emitter self-plays are left alone — their script/animation drives
    // the rest, and restarting those stages early would double-fire them.
    private static void KickDormantParticles(Node root)
    {
        List<GpuParticles2D> emitters = new();
        CollectEmitters(root, emitters);
        if (emitters.Count == 0 || emitters.Any(p => p.Emitting))
        {
            return;
        }
        foreach (GpuParticles2D p in emitters)
        {
            p.Restart();
        }
    }

    private static void CollectEmitters(Node node, List<GpuParticles2D> sink)
    {
        if (node is GpuParticles2D p)
        {
            sink.Add(p);
        }
        foreach (Node child in node.GetChildren())
        {
            CollectEmitters(child, sink);
        }
    }

    // Estimate a scene's visual bounds in the root node's local space by merging
    // GpuParticles2D visibility rects, sprite rects, and control rects down the tree.
    // Approximate by design — visibility rects are authored, not simulated — but good
    // enough to keep each vfx roughly centered on the stage.
    private static void AccumulateBounds(Node node, Transform2D xform, ref Rect2? acc, bool isRoot = false)
    {
        // Bounds are expressed in the ROOT's local space: the root's own transform is
        // excluded (the caller re-applies it via GlobalTransform when recentering).
        Transform2D t = !isRoot && node is CanvasItem item ? xform * item.GetTransform() : xform;

        Rect2? local = node switch
        {
            GpuParticles2D p => p.VisibilityRect,
            Sprite2D s when s.Texture != null => s.GetRect(),
            Control c => new Rect2(Vector2.Zero, c.Size),
            _ => null,
        };
        if (local.HasValue)
        {
            Rect2 r = local.Value;
            foreach (Vector2 corner in new[]
                     {
                         r.Position,
                         r.Position + new Vector2(r.Size.X, 0),
                         r.Position + new Vector2(0, r.Size.Y),
                         r.End,
                     })
            {
                Vector2 p = t * corner;
                acc = acc.HasValue ? acc.Value.Expand(p) : new Rect2(p, Vector2.Zero);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            AccumulateBounds(child, t, ref acc);
        }
    }

    // Most vfx scenes free themselves when their particles finish; this catches the ones
    // that don't (looping/overlay scenes) so the stage never accumulates stale nodes.
    private async System.Threading.Tasks.Task FreeAfterFailsafe(Node node, double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(node) && !node.IsQueuedForDeletion())
        {
            node.QueueFree();
        }
    }

    // ---------------------------------------------------------- enumeration --

    private static List<FxEntry> CollectSfxEntries()
    {
        List<FxEntry> entries = new();

        foreach (string ev in CollectFmodEventPaths())
        {
            entries.Add(new FxEntry(ev, ev, ev));
        }

        foreach (string file in ListFiles("res://debug_audio").OrderBy(f => f, StringComparer.Ordinal))
        {
            if (file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                // Copy text is the bare filename — the exact string the tmpSfx params take
                // (.WithHitFx(vfx, sfx, tmpSfx) / NDebugAudioManager.Play).
                entries.Add(new FxEntry($"debug_audio/{file}", file, file, Tag: "[mp3]"));
            }
        }

        return entries;
    }

    // Ask the FMOD GDExtension (the "FmodServer" engine singleton) for every event
    // description in the loaded banks. Note: only loaded banks are visible — the act music
    // banks load on demand during a run, so music events won't appear here; every sfx bank
    // (Master/sfx/temp_sfx/ambience + strings) is loaded at startup. The event-path strings
    // are not recoverable by scanning the bank bytes (the strings bank interns/fragments
    // them), so the live API is the only reliable source.
    private static List<string> CollectFmodEventPaths()
    {
        HashSet<string> events = new(StringComparer.Ordinal);
        try
        {
            GodotObject fmod = Engine.GetSingleton("FmodServer");
            foreach (Variant v in fmod.Call("get_all_event_descriptions").AsGodotArray())
            {
                GodotObject? desc = v.AsGodotObject();
                if (desc == null)
                {
                    continue;
                }
                string path = desc.HasMethod("get_path")
                    ? desc.Call("get_path").AsString()
                    : desc.HasMethod("get_event_path")
                        ? desc.Call("get_event_path").AsString()
                        : "";
                if (path.StartsWith("event:/", StringComparison.Ordinal))
                {
                    events.Add(path);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: FmodServer event enumeration failed: {e}");
        }

        if (events.Count == 0)
        {
            MainFile.Logger.Error("FX Lab: no FMOD events found via FmodServer");
        }
        return events.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    private static List<FxEntry> CollectVfxEntries()
    {
        List<FxEntry> entries = new();

        // Base-game vfx scenes. Copy text is the "inner path" the code APIs take
        // (VfxCmd.PlayOnCreatureCenter / .WithHitFx): "vfx/vfx_attack_slash".
        List<string> baseScenes = new();
        CollectFilesRecursive("res://scenes/vfx", baseScenes);
        foreach (string scene in NormalizeSceneFiles(baseScenes))
        {
            string inner = scene["res://scenes/".Length..^".tscn".Length];
            entries.Add(MakeVfxEntry(inner, inner, scene));
        }

        // Mod vfx scenes (if any): copy text is the full res:// path, which is what
        // ResourceLoader.Load in mod code takes.
        List<string> modScenes = new();
        CollectFilesRecursive(MainFile.ResPath, modScenes);
        foreach (string scene in NormalizeSceneFiles(modScenes))
        {
            if (scene.Contains("vfx", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(MakeVfxEntry(scene.Replace("res://", ""), scene, scene));
            }
        }

        return entries.OrderBy(e => e.Display, StringComparer.Ordinal).ToList();
    }

    private static FxEntry MakeVfxEntry(string display, string copyText, string scenePath)
    {
        SceneFxInfo info = AnalyzeScene(scenePath);
        string tag;
        double lifetime = VfxFailsafeSeconds;
        if (info.Scripted)
        {
            // Game C# script drives (and usually frees) the effect; may need combat context.
            tag = "[script]";
        }
        else if (info.Emitters > 0 && info.OneShotEmitters < info.Emitters)
        {
            tag = "[loop]";
            lifetime = LoopVfxKillSeconds;
        }
        else
        {
            tag = "[one-shot]";
        }
        return new FxEntry(display, copyText, scenePath, lifetime, tag);
    }

    private sealed record SceneFxInfo(int Emitters, int OneShotEmitters, bool Scripted);

    private static readonly Dictionary<string, SceneFxInfo> _sceneInfoCache = new(StringComparer.Ordinal);

    // Classify a vfx scene by reading its .tscn text (scenes ship as text in the pck):
    // count GPUParticles2D emitters vs one_shot flags, note game-code scripts, and fold in
    // instanced sub-scenes recursively. Heuristic, but drives only the list tag + kill time.
    private static SceneFxInfo AnalyzeScene(string scenePath)
    {
        if (_sceneInfoCache.TryGetValue(scenePath, out SceneFxInfo? cached))
        {
            return cached;
        }
        _sceneInfoCache[scenePath] = new SceneFxInfo(0, 0, false); // cycle guard

        string text = Godot.FileAccess.FileExists(scenePath) ? Godot.FileAccess.GetFileAsString(scenePath) : "";
        int emitters = CountOccurrences(text, "type=\"GPUParticles2D\"");
        int oneShot = CountOccurrences(text, "one_shot = true");
        bool scripted = System.Text.RegularExpressions.Regex.IsMatch(
            text, "\\[ext_resource type=\"Script\"[^\\]]*path=\"res://src/");

        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                     text, "\\[ext_resource type=\"PackedScene\"[^\\]]*path=\"([^\"]+)\""))
        {
            SceneFxInfo sub = AnalyzeScene(m.Groups[1].Value);
            emitters += sub.Emitters;
            oneShot += sub.OneShotEmitters;
            scripted |= sub.Scripted;
        }

        SceneFxInfo info = new(emitters, oneShot, scripted);
        _sceneInfoCache[scenePath] = info;
        return info;
    }

    private static int CountOccurrences(string text, string needle)
    {
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }

    // In an exported .pck, .tscn files may be listed as ".tscn.remap"; the original path
    // still loads through the remap. Strip the suffix and dedupe.
    private static IEnumerable<string> NormalizeSceneFiles(IEnumerable<string> files)
    {
        return files
            .Select(f => f.EndsWith(".remap", StringComparison.Ordinal) ? f[..^".remap".Length] : f)
            .Where(f => f.EndsWith(".tscn", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(f => f, StringComparer.Ordinal);
    }

    private static IEnumerable<string> ListFiles(string dirPath)
    {
        using DirAccess? dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            return Array.Empty<string>();
        }
        return dir.GetFiles()
            .Select(f => f.EndsWith(".import", StringComparison.Ordinal) || f.EndsWith(".remap", StringComparison.Ordinal)
                ? f[..f.LastIndexOf('.')]
                : f)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static void CollectFilesRecursive(string dirPath, List<string> sink)
    {
        using DirAccess? dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            return;
        }
        foreach (string file in dir.GetFiles())
        {
            sink.Add($"{dirPath}/{file}");
        }
        foreach (string sub in dir.GetDirectories())
        {
            CollectFilesRecursive($"{dirPath}/{sub}", sink);
        }
    }
}
