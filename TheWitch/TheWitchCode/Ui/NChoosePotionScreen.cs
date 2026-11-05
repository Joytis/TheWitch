using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Ui;

/// <summary>
/// Mid-combat "choose a potion" overlay: a grid of <see cref="NPotionChoiceCell" />s (4 columns, widening
/// to 8 for big pools) under the base-game common banner, pushed on <see cref="NOverlayStack" /> (shared
/// dim backstop). Structure cloned from NChooseARelicSelection; layout authored in choose_potion_screen.tscn
/// (this script is its root): banner + a full-width band from 60% viewport height to the bottom, with the
/// grid shrink-centered inside it so rows sit top-center under the title and grow downward. The band is a
/// base-game <see cref="NScrollableContainer" /> (Scroll/Mask/Content + Scrollbar, the potion-lab shape):
/// wheel/drag/scrollbar scroll, controller focus auto-scrolls; inert while the grid fits. Needed because
/// other mods' shared potions can grow the pool past one screen. Force-pick: there is no Skip button — the
/// screen resolves only by choosing. Await <see cref="PotionSelected" />. Show via
/// <see cref="PotionSelectCmd.FromChoosePotionScreen" /> for the MP-synced flow, not directly.
/// </summary>
[GlobalClass]
public partial class NChoosePotionScreen : Control, IOverlayScreen
{
    private const int _minColumns = 4;
    private const int _maxColumns = 8;
    // Columns grow once the 4-wide grid would pass this many rows, so big pools spread sideways first.
    private const int _rowsBeforeWidening = 4;

    public static readonly string scenePath = "ui/choose_potion_screen.tscn".CharacterScenePath();

    private readonly TaskCompletionSource<PotionModel?> _completionSource = new();
    private IReadOnlyList<PotionModel> _potions = null!;
    private LocString _header = null!;
    private GridContainer _grid = null!;
    private NScrollableContainer _scroll = null!;
    private int _columns = _minColumns;
    private NPeekButton _peekButton = null!;
    private Tween? _fadeTween;

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _peekButton.IsPeeking
        ? NCombatRoom.Instance?.DefaultFocusedControl
        : _grid.GetChildOrNull<Control>(0);

    public static NChoosePotionScreen? ShowScreen(IReadOnlyList<PotionModel> potions, LocString header)
    {
        if (TestMode.IsOn || NOverlayStack.Instance == null)
        {
            return null;
        }
        // Instantiate<T> throws when the .tscn script didn't bind — a no-bind scene is a
        // malformed asset; fail loud.
        NChoosePotionScreen screen = PreloadManager.Cache.GetScene(scenePath)
            .Instantiate<NChoosePotionScreen>(PackedScene.GenEditState.Disabled);
        screen.Name = "NChoosePotionScreen";
        screen._potions = potions;
        screen._header = header;
        NOverlayStack.Instance.Push(screen);
        return screen;
    }

    public override void _Ready()
    {
        // Scene-instanced base-game nodes (banner, peek button) require the src/ junction at
        // export — if a script fails to bind, GetNode<T> throws right here. Fail loud.
        NCommonBanner banner = GetNode<NCommonBanner>("%Banner");
        banner.label.SetTextAutoSize(_header.GetRawText());
        banner.AnimateIn();

        _scroll = GetNode<NScrollableContainer>("%Scroll");
        _scroll.DisableScrollingIfContentFits();
        _grid = GetNode<GridContainer>("%Grid");
        _columns = Math.Clamp((_potions.Count + _rowsBeforeWidening - 1) / _rowsBeforeWidening, _minColumns, _maxColumns);
        _grid.Columns = _columns;

        foreach (PotionModel potion in _potions)
        {
            NPotionChoiceCell? cell = NPotionChoiceCell.Create(potion);
            _grid.AddChild(cell);
            cell!.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => SelectCell(cell)));
            CreateTween()
                .TweenProperty(cell, "modulate", Colors.White, 1.0)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic)
                .From(Colors.Black);
        }

        WireControllerFocus();
        SetupPeekButton(banner);
    }

    /// <summary>
    /// The base-game peek button (eye icon, hides the overlay to show combat state) that every
    /// base-game selection screen carries — wiring mirrors NCardGridSelectionScreen.
    /// </summary>
    private void SetupPeekButton(NCommonBanner banner)
    {
        NPeekButton peek = GetNode<NPeekButton>("%PeekButton");
        _peekButton = peek;
        peek.AddTargets(_scroll, banner);
        peek.Connect(NPeekButton.SignalName.Toggled, Callable.From<NPeekButton>(_ =>
        {
            if (peek.IsPeeking)
            {
                MouseFilter = MouseFilterEnum.Ignore;
            }
            else
            {
                MouseFilter = MouseFilterEnum.Stop;
                ActiveScreenContext.Instance.Update();
            }
        }));
    }

    /// <summary>Non-wrapping grid navigation; a partial last row is reachable from anywhere above it.</summary>
    private void WireControllerFocus()
    {
        List<NPotionChoiceCell> cells = _grid.GetChildren().OfType<NPotionChoiceCell>().ToList();
        int last = cells.Count - 1;
        for (int i = 0; i < cells.Count; i++)
        {
            Control cell = cells[i];
            int col = i % _columns;
            Control below = i + _columns <= last
                ? cells[i + _columns]
                : (i / _columns < last / _columns ? cells[last] : cell);
            cell.FocusNeighborLeft = (col > 0 ? cells[i - 1] : cell).GetPath();
            cell.FocusNeighborRight = (col < _columns - 1 && i < last ? cells[i + 1] : cell).GetPath();
            cell.FocusNeighborTop = (i - _columns >= 0 ? cells[i - _columns] : cell).GetPath();
            cell.FocusNeighborBottom = below.GetPath();
        }
    }

    private void SelectCell(NPotionChoiceCell cell)
    {
        if (!_completionSource.Task.IsCompleted)
        {
            _completionSource.SetResult(cell.Model);
        }
    }

    public async Task<PotionModel?> PotionSelected()
    {
        PotionModel? result = await _completionSource.Task;
        NOverlayStack.Instance?.Remove(this);
        return result;
    }

    public override void _ExitTree()
    {
        if (!_completionSource.Task.IsCompleted)
        {
            _completionSource.SetCanceled();
        }
    }

    public void AfterOverlayOpened()
    {
        Modulate = Colors.Transparent;
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1f, 0.2);
        // Explicit default-focus grab on open (the rewards screen does the same). No-op on mouse+keyboard.
        DefaultFocusedControl?.TryGrabFocus();
    }

    public void AfterOverlayClosed()
    {
        _peekButton.SetPeeking(false);
        _fadeTween?.Kill();
        this.QueueFreeSafely();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
        if (CombatManager.Instance.IsInProgress)
        {
            _peekButton.Enable();
        }
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
        _peekButton.Disable();
    }
}
