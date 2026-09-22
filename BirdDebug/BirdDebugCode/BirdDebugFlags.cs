using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bird.Common;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Bird.BirdDebug;

/// <summary>
/// Launch-option debug helpers (dev-only BirdDebug mod). Patches are only applied when the matching
/// argument is present, so a normal launch is untouched. The flags are the repo-wide "bird" family;
/// they act on whichever character mod <c>-bird-character=&lt;key&gt;</c> names (absent = Witch, see
/// <see cref="BirdCharacterArg"/>), resolved lazily through <see cref="BirdCharacter"/>.
///   --bird-debug                     patches NGame.IsReleaseGame to false, unlocking dev-only
///                                     features gated on it (notably the -autoslay smoke-test bot).
///                                     With -autoslay also present, the bot is forced onto the
///                                     selected character.
///   --bird-bootstrap[=ENCOUNTER_ID]  skips the main menu and launches a fresh run of the selected
///                                     character straight into a combat with 100 max energy. Optional
///                                     value picks the encounter (default CULTISTS_NORMAL); -seed &lt;s&gt;
///                                     is honored.
///   --bird-fxlab                     skips the main menu and opens the FX Lab (NFxLab):
///                                     searchable SFX/VFX browser with play + copy-path buttons.
///                                     Pair with --bird-debug. Wins over --bird-bootstrap.
///   --bird-iconlab                   skips the main menu and opens the Icon Lab (NIconLab):
///                                     every relic + potion, the selected character's above base
///                                     game, drawn in each composited state (owned / not seen /
///                                     undiscovered / locked / raw outline) for art-parity checks.
///   --bird-cardtest                  headless smoke test: at main-menu ready, plays every card of
///                                     the selected character's mod in a throwaway test combat
///                                     (CardTest) and logs failures to the autoslay log. -seed &lt;s&gt;
///                                     is honored.
///   --bird-potiontest                same harness: procures + uses + discards every mod potion.
///   --bird-relictest                 same harness: equips EVERY mod relic, then runs the card
///                                     exercise for every card and the potion exercise for every potion.
///   --bird-menutest                  main-menu sweep: opens every menu screen (character select
///                                     with the selected character picked, card library per pool
///                                     tab, relic collection, potion lab, bestiary, stats, run
///                                     history, timeline, settings, profile) and renders every mod
///                                     hover tip. Catches render-time faults the combat harness misses.
///   --bird-testall                   menu sweep, then cards, potions, relics — all in one process.
///   --bird-reset-ftue                at main-menu ready, forgets every mod FTUE (progress-save
///                                     keys prefixed with the character's model-id prefix, e.g.
///                                     "thewitch_") and re-enables tutorials, then saves — so a mod
///                                     tip (e.g. the Unstable potion tip) shows again. Base-game
///                                     FTUEs are left alone. Composable with any other flag.
///   --bird-test-update-popup         shows the Workshop self-update "restart required" popup
///                                     directly at the main menu (no Steam calls) — popup UI/loc
///                                     iteration. Handled in each character mod's Common/
///                                     WorkshopSelfUpdate.Initialize, NOT here.
///   --bird-force-workshop-download[=ITEMID]
///                                     skips the staleness gate and forces the high-priority
///                                     Workshop download + monitor + popup path. The optional
///                                     ITEMID lets a local mods/-folder build target the live
///                                     Workshop item. Handled in Common/WorkshopSelfUpdate, NOT here.
///
/// Patch-point note: NGame.GameStartup's state machine is already JIT-compiled (and possibly
/// tier-1 promoted with call sites inlined) by the time mods initialize inside it, so methods it
/// calls (e.g. LaunchMainMenu) cannot be reliably detoured from here. We therefore hook
/// NMainMenu._Ready / NCharacterSelectButton.Select — both compile after mod init.
/// </summary>
public static class BirdDebug
{
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(MainFile.ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private const string DefaultEncounter = "CULTISTS_NORMAL";
    private const decimal BootstrapMaxEnergy = 100m;

    private static bool _bootstrapStarted;
    private static bool _fxLabStarted;
    private static bool _iconLabStarted;
    private static bool _cardTestStarted;
    private static CardTest.Mode _smokeTestMode;

    private static bool TryGetSmokeTestMode(out CardTest.Mode mode)
    {
        if (CommandLineHelper.HasArg("bird-cardtest")) { mode = CardTest.Mode.Cards; return true; }
        if (CommandLineHelper.HasArg("bird-potiontest")) { mode = CardTest.Mode.Potions; return true; }
        if (CommandLineHelper.HasArg("bird-relictest")) { mode = CardTest.Mode.Relics; return true; }
        if (CommandLineHelper.HasArg("bird-menutest")) { mode = CardTest.Mode.Menu; return true; }
        if (CommandLineHelper.HasArg("bird-testall")) { mode = CardTest.Mode.All; return true; }
        mode = default;
        return false;
    }

    public static void ApplyPatches(Harmony harmony)
    {
        Logger.Info($"-{BirdCharacterArg.Arg}={BirdCharacter.Key}: bird debug flags target this character");

        if (CommandLineHelper.HasArg("bird-debug"))
        {
            // DebugSettings.DevSkip latches STS2_DEV_SKIP on first access, which happens after
            // mod init (NGame's skipLogo check) — so setting it here skips the intro logo and
            // the timeline prompt for every -bird-debug launch, no launcher env var needed.
            System.Environment.SetEnvironmentVariable("STS2_DEV_SKIP", "1");

            Logger.Info("--bird-debug: patching NGame.IsReleaseGame to false");
            harmony.Patch(
                AccessTools.Method(typeof(NGame), nameof(NGame.IsReleaseGame)),
                prefix: new HarmonyMethod(typeof(BirdDebug), nameof(IsReleaseGamePrefix)));

            if (CommandLineHelper.HasArg("autoslay"))
            {
                Logger.Info($"--bird-debug + -autoslay: forcing {BirdCharacter.Key} in character select");
                harmony.Patch(
                    AccessTools.Method(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Select)),
                    prefix: new HarmonyMethod(typeof(BirdDebug), nameof(CharacterSelectPrefix)));
            }
        }

        if (CommandLineHelper.HasArg("bird-reset-ftue"))
        {
            Logger.Info($"--bird-reset-ftue: will clear {BirdCharacter.Key} FTUE flags at the main menu");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(ResetFtueMenuReadyPostfix)));
        }

        if (CommandLineHelper.HasArg("bird-fxlab"))
        {
            Logger.Info("--bird-fxlab: will skip menu and open the FX Lab");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(FxLabMenuReadyPostfix)));
        }
        else if (CommandLineHelper.HasArg("bird-iconlab"))
        {
            Logger.Info("--bird-iconlab: will skip menu and open the Icon Lab");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(IconLabMenuReadyPostfix)));
        }
        else if (TryGetSmokeTestMode(out CardTest.Mode mode))
        {
            _smokeTestMode = mode;
            Logger.Info($"{CardTest.TagFor(mode)}: will run the headless {mode} smoke test at the main menu");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(CardTestMenuReadyPostfix)));
        }
        else if (CommandLineHelper.HasArg("bird-bootstrap"))
        {
            Logger.Info("--bird-bootstrap: will skip menu and enter combat directly");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(MainMenuReadyPostfix)));
            harmony.Patch(
                AccessTools.Method(typeof(Hook), nameof(Hook.ModifyMaxEnergy)),
                postfix: new HarmonyMethod(typeof(BirdDebug), nameof(ModifyMaxEnergyPostfix)));
        }
    }

    private static bool IsReleaseGamePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void ModifyMaxEnergyPostfix(ref decimal __result)
    {
        __result = BootstrapMaxEnergy;
    }

    // AutoSlay picks a random unlocked character button; redirect its pick to the selected
    // character. Only steers the bot — manual character select is untouched.
    private static bool CharacterSelectPrefix(NCharacterSelectButton __instance)
    {
        if (!AutoSlayer.IsActive)
        {
            return true;
        }
        ModelId wanted = BirdCharacter.Model.Id;
        if (__instance.Character.Id == wanted)
        {
            return true;
        }
        NCharacterSelectButton? button = __instance.GetParent()
            .GetChildren()
            .OfType<NCharacterSelectButton>()
            .FirstOrDefault(b => b.Character.Id == wanted && !b.IsLocked);
        if (button == null)
        {
            Logger.Error($"autoslay: no unlocked {BirdCharacter.Key} button found; letting the random pick stand");
            return true;
        }
        Logger.Info($"autoslay: redirecting character select to {BirdCharacter.Key} (was {__instance.Character.Id})");
        button.Select();
        return false;
    }

    /// <summary>
    /// Drops every "&lt;prefix&gt;*" key (e.g. "thewitch_*") from the progress save's completed-FTUE set
    /// (private HashSet on ProgressState — no public per-key API; ResetFtues() would wipe base-game
    /// tutorials too), flips tutorials back on, and saves.
    /// </summary>
    private static void ResetFtueMenuReadyPostfix()
    {
        string prefix = BirdCharacter.FtuePrefix;
        ProgressState progress = SaveManager.Instance.Progress;
        HashSet<string> completed = AccessTools.FieldRefAccess<ProgressState, HashSet<string>>("_ftueCompleted")(progress);
        int removed = completed.RemoveWhere(k => k.StartsWith(prefix, StringComparison.Ordinal));
        progress.EnableFtues = true;
        SaveManager.Instance.SaveProgressFile();
        Logger.Info($"--bird-reset-ftue: cleared {removed} {BirdCharacter.Key} FTUE flag(s) ('{prefix}*'); tutorials enabled");
    }

    private static void FxLabMenuReadyPostfix(NMainMenu __instance)
    {
        if (_fxLabStarted)
        {
            return;
        }
        _fxLabStarted = true;
        Logger.Info("--bird-fxlab: main menu ready, opening the FX Lab");
        TaskHelper.RunSafely(OpenFxLab(__instance));
    }

    private static async Task OpenFxLab(NMainMenu menu)
    {
        // Same settle-delay as the combat bootstrap: let NGame's startup finish first.
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                Logger.Error("--bird-fxlab failed: NGame.Instance is null");
                return;
            }
            game.RootSceneContainer.SetCurrentScene(NFxLab.Create());
        }
        catch (Exception e)
        {
            Logger.Error($"--bird-fxlab failed: {e}");
        }
    }

    private static void IconLabMenuReadyPostfix(NMainMenu __instance)
    {
        if (_iconLabStarted)
        {
            return;
        }
        _iconLabStarted = true;
        Logger.Info("--bird-iconlab: main menu ready, opening the Icon Lab");
        TaskHelper.RunSafely(OpenIconLab(__instance));
    }

    private static async Task OpenIconLab(NMainMenu menu)
    {
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                Logger.Error("--bird-iconlab failed: NGame.Instance is null");
                return;
            }
            game.RootSceneContainer.SetCurrentScene(NIconLab.Create());
        }
        catch (Exception e)
        {
            Logger.Error($"--bird-iconlab failed: {e}");
        }
    }

    private static void CardTestMenuReadyPostfix(NMainMenu __instance)
    {
        if (_cardTestStarted)
        {
            return;
        }
        _cardTestStarted = true;
        TaskHelper.RunSafely(RunCardTest(__instance));
    }

    private static async Task RunCardTest(NMainMenu menu)
    {
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        string? seed = CommandLineHelper.GetValue("seed");
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = SeedHelper.GetRandomSeed();
        }
        // Smoke tests are unattended: silence the game (menu music + every UI/hover sfx). The
        // volume setters no-op under TestMode, so mute before the combat harness turns it on.
        NAudioManager.Instance?.SetMasterVol(0f);
        try
        {
            int priorTotal = 0;
            List<(string item, Exception ex)>? priorFailures = null;
            if (_smokeTestMode is CardTest.Mode.Menu or CardTest.Mode.All)
            {
                (priorTotal, priorFailures) = await MenuTest.Run(menu);
                if (_smokeTestMode == CardTest.Mode.Menu)
                {
                    // Same exit-code contract as the combat harness: 0 = all passed, 1 = failures.
                    NGame.Instance?.GetTree().Quit(priorFailures.Count == 0 ? 0 : 1);
                    return;
                }
            }
            await CardTest.RunAll(seed, _smokeTestMode, priorTotal, priorFailures);
        }
        catch (Exception e)
        {
            Logger.Error($"{CardTest.TagFor(_smokeTestMode)} failed: {e}");
        }
    }

    private static void MainMenuReadyPostfix(NMainMenu __instance)
    {
        if (_bootstrapStarted)
        {
            return;
        }
        _bootstrapStarted = true;
        Logger.Info("--bird-bootstrap: main menu ready, starting combat bootstrap");
        TaskHelper.RunSafely(BootstrapFromMenu(__instance));
    }

    private static async Task BootstrapFromMenu(NMainMenu menu)
    {
        // Let NGame's startup step past the menu await before replacing the scene.
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 2; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                Logger.Error("--bird-bootstrap failed: NGame.Instance is null");
                return;
            }
            await StartCombatRun(game);
        }
        catch (Exception e)
        {
            Logger.Error($"--bird-bootstrap failed: {e}");
        }
    }

    // Uses the same path as a real new run (NGame.StartNewSingleplayerRun -> StartRun: run+act
    // assets, starting relics, EnterAct), then jumps to the encounter like the `fight` console cmd.
    private static async Task StartCombatRun(NGame game)
    {
        NAudioManager.Instance?.StopMusic();

        CharacterModel character = BirdCharacter.Model;
        string seed = CommandLineHelper.GetValue("seed") ?? SeedHelper.GetRandomSeed();

        await game.StartNewSingleplayerRun(
            character,
            shouldSave: false,
            ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(),
            seed,
            GameMode.Standard);

        // GetValue can yield "" (not just null) when the arg has no value — fall back on both.
        string? encounterArg = CommandLineHelper.GetValue("bird-bootstrap");
        string entry = (string.IsNullOrWhiteSpace(encounterArg) ? DefaultEncounter : encounterArg).ToUpperInvariant();
        ModelId modelId = new(ModelId.SlugifyCategory<EncounterModel>(), entry);
        EncounterModel encounter = ModelDb.GetById<EncounterModel>(modelId).ToMutable();
        encounter.DebugRandomizeRng();
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Unassigned, encounter);

        Logger.Info($"--bird-bootstrap: entered encounter '{entry}' with seed '{seed}' as {character.Id}");
    }
}
