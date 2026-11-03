using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Bird.BirdDebug;

/// <summary>
/// Workshop marketing screenshots (--bird-shot=&lt;name&gt;): stages one scene of the selected character,
/// waits for it to settle, saves the window as a PNG (optionally cropped) and quits. Driven per shot by
/// tools/capture-marketing.ps1, which reads &lt;workshop&gt;/marketing/screenshots.json.
///   --bird-shot=charselect    character select with the character picked (singleplayer screen)
///   --bird-shot=familiars     combat: every "*Familiar" summon card (IFamiliarSummon) of the character's
///                             assembly is played, then a fresh copy of each is put in hand
///   --bird-shot=restsite      a rest site
///   --bird-shot=mp-relics     two-player treasure room ("Choose the Relic YOU Want!"): launch one
///                             instance with -fastmp host_standard and one with -fastmp join, both with
///                             this flag. Both pick the character and ready up; the game's own
///                             IBootstrapSettings path drops the lobby straight into a treasure room;
///                             both open the chest; the HOST points both hands at the relics and shoots.
///                             The client just idles until the launcher closes it.
///   --bird-shot-out=&lt;path.png&gt;    where to write (required on the shooting instance)
///   --bird-shot-crop=x,y,w,h       crop rect in window pixels, applied before saving
///   --bird-shot-size=WxH           window size to render at (default 1920x1080)
/// While a shot runs, FTUEs are treated as seen (no tutorial popups) and the game is muted. Exit code:
/// 0 = written, 1 = failed (logged with the [bird-shot] tag).
/// </summary>
public static class MarketingShots
{
    public const string Tag = "[bird-shot]";
    private static readonly TimeSpan Watchdog = TimeSpan.FromMinutes(3);

    private static string _shot = "";
    private static bool _started;

    public static bool TryApply(Harmony harmony)
    {
        string? shot = CommandLineHelper.GetValue("bird-shot");
        if (string.IsNullOrWhiteSpace(shot))
        {
            return false;
        }
        _shot = shot.Trim().ToLowerInvariant();
        BirdDebug.Logger.Info($"{Tag} will stage '{_shot}' for {BirdCharacter.Key}");

        harmony.Patch(AccessTools.Method(typeof(SaveManager), nameof(SaveManager.SeenFtue)),
            prefix: new HarmonyMethod(typeof(MarketingShots), nameof(SeenFtuePrefix)));

        if (_shot == "mp-relics")
        {
            harmony.Patch(AccessTools.Method(typeof(NCharacterSelectScreen), "_Ready"),
                postfix: new HarmonyMethod(typeof(MarketingShots), nameof(CharacterSelectReadyPostfix)));
            harmony.Patch(AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened)),
                postfix: new HarmonyMethod(typeof(MarketingShots), nameof(CharacterSelectOpenedPostfix)));
        }
        else
        {
            harmony.Patch(AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(MarketingShots), nameof(MainMenuReadyPostfix)));
        }
        return true;
    }

    private static bool SeenFtuePrefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    // ---- solo shots: start from the main menu ----------------------------------------------------

    private static void MainMenuReadyPostfix(NMainMenu __instance)
    {
        if (_started)
        {
            return;
        }
        _started = true;
        TaskHelper.RunSafely(Run(() => _shot switch
        {
            "charselect" => CharacterSelect(__instance),
            "familiars" => Familiars(__instance),
            "restsite" => RestSite(__instance),
            _ => throw new ArgumentException($"unknown shot '{_shot}' (charselect | familiars | restsite | mp-relics)"),
        }));
    }

    private static async Task CharacterSelect(NMainMenu menu)
    {
        NMainMenuSubmenuStack stack = menu.SubmenuStack;
        stack.PushSubmenuType<NSingleplayerSubmenu>();
        await Frames(10);
        NCharacterSelectScreen select = stack.GetSubmenuType<NCharacterSelectScreen>();
        select.InitializeSingleplayer();
        stack.Push(select);
        await Frames(10);
        CharacterButton(select).Select();
        await Seconds(4);
        await Capture();
    }

    private static async Task Familiars(NMainMenu menu)
    {
        await StartRun();
        EncounterModel encounter = ModelDb.GetById<EncounterModel>(new ModelId(ModelId.SlugifyCategory<EncounterModel>(), "CULTISTS_NORMAL")).ToMutable();
        encounter.DebugRandomizeRng();
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Unassigned, encounter);

        Stopwatch sw = Stopwatch.StartNew();
        Player? player = null;
        while (sw.Elapsed < TimeSpan.FromSeconds(30))
        {
            player = CombatManager.Instance.DebugOnlyGetState() is { } state ? LocalContext.GetMe(state) : null;
            if (player?.PlayerCombatState?.Phase == PlayerTurnPhase.Play)
            {
                break;
            }
            await Frames(1);
        }
        if (player?.PlayerCombatState?.Phase != PlayerTurnPhase.Play || CombatManager.Instance.DebugOnlyGetState() is not { } combat)
        {
            throw new InvalidOperationException("combat never reached the play phase");
        }
        await Seconds(2);   // turn-start banner + opening draw

        List<CardModel> summons = ModelDb.AllCards
            .Where(c => BirdCharacter.Owns(c) && c.GetType().Name.EndsWith("Familiar", StringComparison.Ordinal)
                        && c.GetType().GetInterfaces().Any(i => i.Name == "IFamiliarSummon"))
            .OrderBy(c => c.GetType().Name)
            .ToList();
        if (summons.Count == 0)
        {
            throw new InvalidOperationException($"{BirdCharacter.Key} has no *Familiar IFamiliarSummon cards");
        }
        BirdDebug.Logger.Info($"{Tag} summoning {string.Join(", ", summons.Select(c => c.Id.Entry))}");

        BlockingPlayerChoiceContext ctx = new();
        await CardCmd.Discard(ctx, player.PlayerCombatState!.Hand.Cards.ToList());
        foreach (CardModel summon in summons)
        {
            CardModel card = combat.CreateCard(summon, player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            await CardCmd.AutoPlay(ctx, card, null);
            await Seconds(0.6);
        }
        foreach (CardModel summon in summons)
        {
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(summon, player), PileType.Hand, player);
        }
        await Seconds(3);
        await Capture();
    }

    private static async Task RestSite(NMainMenu menu)
    {
        await StartRun();
        await RunManager.Instance.EnterRoomDebug(RoomType.RestSite);
        RunManager.Instance.ActionExecutor.Unpause();
        await Seconds(4);
        await Capture();
    }

    private static async Task StartRun()
    {
        await Frames(2);
        await Game.StartNewSingleplayerRun(BirdCharacter.Model, shouldSave: false, ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(), "BIRDSHOT", GameMode.Standard);
    }

    // ---- multiplayer relic shot: -fastmp lobby -> bootstrap treasure room -----------------------

    // Hands the screen the game's own multiplayer bootstrap, so BeginRun drops every lobby member
    // into a treasure room (NCharacterSelectScreen.StartNewMultiplayerRun, BootstrapInMultiplayer).
    private static void CharacterSelectReadyPostfix(NCharacterSelectScreen __instance)
    {
        AccessTools.Field(typeof(NCharacterSelectScreen), "_settings").SetValue(__instance, new TreasureBootstrap());
    }

    private static void CharacterSelectOpenedPostfix(NCharacterSelectScreen __instance)
    {
        if (_started)
        {
            return;
        }
        _started = true;
        TaskHelper.RunSafely(Run(() => MultiplayerRelics(__instance)));
    }

    private static async Task MultiplayerRelics(NCharacterSelectScreen select)
    {
        bool host = CommandLineHelper.GetValue("fastmp")?.StartsWith("host", StringComparison.Ordinal) == true;
        Stopwatch sw = Stopwatch.StartNew();
        while (select.Lobby == null || (host && select.Lobby.Players.Count < 2))
        {
            if (sw.Elapsed > TimeSpan.FromSeconds(60))
            {
                throw new TimeoutException("the second player never joined the lobby");
            }
            await Frames(5);
        }
        await Seconds(1);
        CharacterButton(select).Select();
        await Seconds(1);
        select.Lobby.SetReady(true);
        BirdDebug.Logger.Info($"{Tag} {(host ? "host" : "client")} picked {BirdCharacter.Key} and readied up");

        NTreasureRoom room = await WaitFor<NTreasureRoom>(TimeSpan.FromSeconds(60));
        await Seconds(2);
        NButton chest = room.GetNode<NButton>("%Chest");
        chest.EmitSignal(NClickableControl.SignalName.Released, chest);
        await Seconds(4);

        if (!host)
        {
            BirdDebug.Logger.Info($"{Tag} client: chest open, idling until the launcher closes this instance");
            return;
        }

        NHandImageCollection hands = Descendants<NHandImageCollection>(room).FirstOrDefault()
                                     ?? throw new InvalidOperationException("no NHandImageCollection in the treasure room");
        List<NTreasureRoomRelicHolder> holders = Descendants<NTreasureRoomRelicHolder>(room).Where(h => h.IsVisibleInTree()).ToList();
        IRunState run = RunManager.Instance.DebugOnlyGetState() ?? throw new InvalidOperationException("no run state");
        for (int i = 0; i < run.Players.Count && holders.Count > 0; i++)
        {
            NTreasureRoomRelicHolder holder = holders[i % holders.Count];
            Vector2 target = holder.GlobalPosition + holder.Size * 0.5f;
            hands.GetHand(run.Players[i].NetId)?.SetPointingPosition(target - hands.GlobalPosition);
        }
        await Seconds(2);
        await Capture();
    }

    private sealed class TreasureBootstrap : IBootstrapSettings
    {
        public CharacterModel Character => BirdCharacter.Model;
        public RoomType RoomType => RoomType.Treasure;
        public EncounterModel Encounter => null!;
        public EventModel Event => null!;
        public ActModel Act => ActModel.GetDefaultList().First();
        public int Ascension => 0;
        public bool SaveRunHistory => false;
        public string? Seed => "BIRDSHOT";
        public bool DoPreloading => true;
        public bool BootstrapInMultiplayer => true;
        public List<ModifierModel> Modifiers => [];
        public Task Setup(Player localPlayer) => Task.CompletedTask;
    }

    // ---- shared ------------------------------------------------------------------------------------

    private static async Task Run(Func<Task> shot)
    {
        NAudioManager.Instance?.SetMasterVol(0f);
        Vector2I size = ParseSize(CommandLineHelper.GetValue("bird-shot-size")) ?? new Vector2I(1920, 1080);
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(size);

        Task work = shot();
        Task finished = await Task.WhenAny(work, Task.Delay(Watchdog));
        int code = 0;
        if (finished != work)
        {
            BirdDebug.Logger.Error($"{Tag} FAILED {_shot}: timed out after {Watchdog.TotalMinutes} minutes");
            code = 1;
        }
        else if (work.Exception is { } e)
        {
            BirdDebug.Logger.Error($"{Tag} FAILED {_shot}: {e.InnerException ?? e}");
            code = 1;
        }
        // The multiplayer client has nothing to write; it stays up until the launcher closes it.
        if (code != 0 || !string.IsNullOrWhiteSpace(CommandLineHelper.GetValue("bird-shot-out")))
        {
            Game.GetTree().Quit(code);
        }
    }

    private static async Task Capture()
    {
        string? path = CommandLineHelper.GetValue("bird-shot-out")?.Trim('"');
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("--bird-shot-out=<path.png> is required");
        }
        // Keep the OS cursor off the window so nothing is hovered.
        Input.WarpMouse(new Vector2(-200, -200));
        await Frames(3);
        await Game.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = Game.GetViewport().GetTexture().GetImage();
        string? crop = CommandLineHelper.GetValue("bird-shot-crop");
        if (!string.IsNullOrWhiteSpace(crop))
        {
            int[] r = crop.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            image = image.GetRegion(new Rect2I(r[0], r[1], r[2], r[3]));
        }
        Error err = image.SavePng(path);
        if (err != Error.Ok)
        {
            throw new InvalidOperationException($"SavePng({path}) failed: {err}");
        }
        BirdDebug.Logger.Info($"{Tag} wrote {path} ({image.GetWidth()}x{image.GetHeight()})");
    }

    private static NCharacterSelectButton CharacterButton(Node select)
    {
        NCharacterSelectButton? button = Descendants<NCharacterSelectButton>(select)
            .FirstOrDefault(b => b.Character.Id == BirdCharacter.Model.Id);
        return button ?? throw new InvalidOperationException($"no {BirdCharacter.Key} button in character select");
    }

    private static async Task<T> WaitFor<T>(TimeSpan timeout) where T : Node
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (Descendants<T>(Game.GetTree().Root).FirstOrDefault() is { } found)
            {
                return found;
            }
            await Frames(5);
        }
        throw new TimeoutException($"no {typeof(T).Name} appeared within {timeout.TotalSeconds}s");
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T t)
            {
                yield return t;
            }
            foreach (T nested in Descendants<T>(child))
            {
                yield return nested;
            }
        }
    }

    // Scene nodes (main menu, character select, rooms) are freed as the shot moves on; NGame lives
    // for the whole process, so every wait, capture and quit anchors on it.
    private static NGame Game => NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null");

    private static async Task Frames(int n)
    {
        SceneTree tree = Game.GetTree();
        for (int i = 0; i < n; i++)
        {
            await Game.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task Seconds(double s)
    {
        await Game.ToSignal(Game.GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout);
    }

    private static Vector2I? ParseSize(string? s)
    {
        string[] p = (s ?? "").ToLowerInvariant().Split('x');
        return p.Length == 2 && int.TryParse(p[0], out int w) && int.TryParse(p[1], out int h) ? new Vector2I(w, h) : null;
    }
}
