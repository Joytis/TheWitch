using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TheAugur.TheAugurCode.Extensions;
using TheAugur.TheAugurCode.Powers;

namespace TheAugur.TheAugurCode.Nodes;

/// <summary>
/// The Portent rifts floating over the Augur: one <see cref="NPortent"/> per foretold card, fanned in
/// an arc above the creature (the base-game Defect orb layout), ordered by remaining turns (soonest
/// first / leftmost). Code-only node, spawned by <see cref="ForetellPower"/> on apply and dismissed on
/// remove; positioned from the creature visuals' OrbPosition like <c>NOrbManager</c>.
/// </summary>
public partial class NForetellManager : Control
{
    private const float Radius = 250f;
    private const float FanDegrees = 100f;
    private const float TweenSeconds = 0.4f;

    private ForetellPower _power = null!;
    private NCreature _creature = null!;
    private bool _isLocal;
    private readonly Dictionary<CardModel, NPortent> _nodes = new();
    private Tween? _tween;

    public static NForetellManager Create(ForetellPower power, NCreature creature)
    {
        NForetellManager manager = new()
        {
            _power = power,
            _creature = creature,
            _isLocal = LocalContext.IsMe(creature.Entity),
            Name = "ForetellManager",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        return manager;
    }

    public override void _Ready()
    {
        UpdatePosition();
        _power.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _power.Changed -= Refresh;
    }

    /// <summary>Fade everything out and free; called when the power leaves the creature.</summary>
    public void Dismiss()
    {
        if (!IsInsideTree())
        {
            this.QueueFreeSafely();
            return;
        }
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(() => this.QueueFreeSafely()));
    }

    private void UpdatePosition()
    {
        NCreatureVisuals visuals = _creature.Visuals;
        Vector2 scale = visuals.Scale.X > 1f ? Vector2.One : visuals.Scale.Lerp(Vector2.One, 0.5f);
        Scale = scale;
        Position = visuals.OrbPosition.Position * Mathf.Min(visuals.Scale.X, 1.25f);
        if (!_isLocal)
        {
            Position += Vector2.Up * 50f;
        }
    }

    private void Refresh()
    {
        IReadOnlyList<ForetellPower.Portent> portents = _power.Portents;
        HashSet<CardModel> live = portents.Select(p => p.Card).ToHashSet();

        // Resolved / gone: fade + free.
        foreach ((CardModel card, NPortent node) in _nodes.ToList())
        {
            if (!live.Contains(card))
            {
                _nodes.Remove(card);
                node.Resolve();
            }
        }
        // New: spawn at the head and fly out to its slot.
        foreach (ForetellPower.Portent portent in portents)
        {
            if (!_nodes.ContainsKey(portent.Card))
            {
                NPortent node = NPortent.Create(portent.Card);
                node.Position = Vector2.Zero;
                node.Modulate = new Color(1f, 1f, 1f, 0f);
                _nodes[portent.Card] = node;
                this.AddChildSafely(node);
            }
            _nodes[portent.Card].SetTurns(portent.TurnsLeft);
        }
        TweenLayout(portents);
    }

    private void TweenLayout(IReadOnlyList<ForetellPower.Portent> portents)
    {
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        int count = portents.Count;
        float radius = _isLocal ? Radius : Radius * 0.75f;
        for (int i = 0; i < count; i++)
        {
            // Arc centred straight above the head; a lone Portent sits at the top.
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float degrees = 90f + FanDegrees / 2f - t * FanDegrees;
            float rad = Mathf.DegToRad(degrees);
            Vector2 target = new(Mathf.Cos(rad) * radius, -Mathf.Sin(rad) * radius);
            NPortent node = _nodes[portents[i].Card];
            _tween.TweenProperty(node, "position", target, TweenSeconds).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            _tween.TweenProperty(node, "modulate:a", 1f, TweenSeconds);
        }
    }
}

/// <summary>
/// One rift: the card's portrait stencilled through a circular mask (clip_children), a tinted ring
/// behind it, and the remaining-turn count. Prototype visuals; the mask is <c>images/ui/portent_mask.png</c>.
/// </summary>
public partial class NPortent : Control
{
    private const float Diameter = 96f;
    private const float RingDiameter = 108f;

    private Label _turns = null!;
    private static readonly Color RingColor = new("e6c15c");

    public static NPortent Create(CardModel card)
    {
        NPortent node = new() { Name = "Portent", MouseFilter = MouseFilterEnum.Ignore };
        node.Build(card);
        return node;
    }

    private void Build(CardModel card)
    {
        Texture2D mask = ResourceLoader.Load<Texture2D>("ui/portent_mask.png".ImagePath());
        Texture2D? portrait = ResourceLoader.Exists(card.PortraitPath) ? ResourceLoader.Load<Texture2D>(card.PortraitPath) : null;

        TextureRect ring = new()
        {
            Texture = mask,
            Modulate = RingColor,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Size = new Vector2(RingDiameter, RingDiameter),
            Position = new Vector2(-RingDiameter / 2f, -RingDiameter / 2f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(ring);

        TextureRect clip = new()
        {
            Texture = mask,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ClipChildren = ClipChildrenMode.Only,
            Size = new Vector2(Diameter, Diameter),
            Position = new Vector2(-Diameter / 2f, -Diameter / 2f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(clip);

        if (portrait != null)
        {
            TextureRect art = new()
            {
                Texture = portrait,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Size = new Vector2(Diameter, Diameter),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            clip.AddChild(art);
        }

        _turns = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(40f, 32f),
            Position = new Vector2(Diameter / 2f - 34f, Diameter / 2f - 26f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _turns.AddThemeFontSizeOverride("font_size", 26);
        _turns.AddThemeColorOverride("font_color", new Color("fff6e2"));
        _turns.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.05f, 0.15f));
        _turns.AddThemeConstantOverride("outline_size", 6);
        if (ResourceLoader.Exists("res://themes/kreon_bold_glyph_space_one.tres"))
        {
            _turns.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres"));
        }
        AddChild(_turns);
    }

    public void SetTurns(int turns) => _turns.Text = turns.ToString();

    /// <summary>The Portent resolved: shrink + fade, then free.</summary>
    public void Resolve()
    {
        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "scale", new Vector2(1.6f, 1.6f), 0.25f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "modulate:a", 0f, 0.25f);
        tween.Chain().TweenCallback(Callable.From(() => this.QueueFreeSafely()));
    }
}
