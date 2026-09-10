using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Nodes.Screens.ProfileScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Main-menu smoke test (--witch-menutest): at main-menu ready, opens every main-menu screen the
/// same way its button would (via <see cref="NMainMenuSubmenuStack"/>), lets it lay out for a few
/// frames, then pops it. Catches render-time content faults the combat harness never sees —
/// pool registration ("You monster!"), missing atlas slices, bad localization keys, hover-tip
/// construction. Also renders the hover tips of every Witch card / relic / potion / power and, in
/// the Card Library, cycles every pool filter (BaseLib adds the mod pool tab). Any exception is a
/// logged failure; stray [ERROR] lines are counted by launch-witch.ps1's report.
/// </summary>
public static class WitchMenuTest
{
    public const string Tag = "[witch-menutest]";
    private const int SettleFrames = 10;

    public static async Task<(int total, List<(string item, Exception ex)> failures)> Run(NMainMenu menu)
    {
        List<(string item, Exception ex)> failures = [];
        int total = 0;
        SceneTree tree = menu.GetTree();
        NMainMenuSubmenuStack stack = menu.SubmenuStack;

        async Task Settle(int frames = SettleFrames)
        {
            for (int i = 0; i < frames; i++)
            {
                await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }

        // Pops back to the bare main menu whatever a step left on the stack.
        async Task Unwind()
        {
            while (stack.SubmenusOpen)
            {
                stack.Pop();
                await Settle(2);
            }
        }

        async Task Step(string name, Func<Task> body)
        {
            total++;
            AutoSlayLog.Info($"{Tag} {name}");
            try
            {
                await body();
                await Settle();
            }
            catch (Exception e)
            {
                Exception actual = e.InnerException ?? e;
                failures.Add((name, actual));
                AutoSlayLog.Error($"{Tag} FAILED {name}: {actual}");
            }
            finally
            {
                NHoverTipSet.Clear();
                try { await Unwind(); }
                catch (Exception e) { AutoSlayLog.Error($"{Tag} FAILED {name} (unwind): {e}"); }
            }
        }

        // Pushes a submenu by type (what the menu button does) and settles.
        async Task Open<T>() where T : NSubmenu
        {
            stack.PushSubmenuType<T>();
            await Settle();
        }

        AutoSlayLog.Action($"{Tag} opening every main-menu screen with the Witch loaded");

        // --- Singleplayer ----------------------------------------------------------
        await Step("Singleplayer", () => Open<NSingleplayerSubmenu>());
        await Step("CharacterSelect", async () =>
        {
            await Open<NSingleplayerSubmenu>();
            NCharacterSelectScreen select = stack.GetSubmenuType<NCharacterSelectScreen>();
            select.InitializeSingleplayer();
            stack.Push(select);
            await Settle();
            NCharacterSelectButton? witch = Descendants<NCharacterSelectButton>(select)
                .FirstOrDefault(b => b.Character is Witch);
            if (witch == null)
            {
                throw new InvalidOperationException("no Witch button in character select");
            }
            if (witch.IsLocked)
            {
                AutoSlayLog.Warn($"{Tag} Witch button is locked; selecting it anyway");
            }
            witch.Select();   // renders the Witch's portrait, starting relic + deck preview
            await Settle(SettleFrames * 2);
        });
        await Step("DailyRun", async () =>
        {
            await Open<NSingleplayerSubmenu>();
            NDailyRunScreen daily = stack.GetSubmenuType<NDailyRunScreen>();
            daily.InitializeSingleplayer();
            stack.Push(daily);
        });
        await Step("CustomRun", async () =>
        {
            await Open<NSingleplayerSubmenu>();
            NCustomRunScreen custom = stack.GetSubmenuType<NCustomRunScreen>();
            custom.InitializeSingleplayer();
            stack.Push(custom);
        });

        // --- Multiplayer (submenu only; no hosting/joining) ------------------------
        await Step("Multiplayer", () => Open<NMultiplayerSubmenu>());

        // --- Compendium ------------------------------------------------------------
        await Step("Compendium", () => Open<NCompendiumSubmenu>());
        await Step("CardLibrary", async () =>
        {
            await Open<NCompendiumSubmenu>();
            NCardLibrary library = stack.GetSubmenuType<NCardLibrary>();
            library.Initialize(null!);   // what NCompendiumSubmenu passes outside a run
            stack.Push(library);
            await Settle(SettleFrames * 2);
            // Cycle every pool tab (base game + the BaseLib-added mod pool) so each filter's
            // card set lays out at least once.
            List<NCardPoolFilter> filters = Descendants<NCardPoolFilter>(library).ToList();
            NCardLibraryGrid? grid = Descendants<NCardLibraryGrid>(library).FirstOrDefault();
            foreach (NCardPoolFilter filter in filters)
            {
                filter.IsSelected = true;
                filter.EmitSignal(NCardPoolFilter.SignalName.Toggled, filter);
                await Settle(5);
                AutoSlayLog.Info($"{Tag}   pool tab {filter.Name}: {grid?.VisibleCards.Count() ?? -1} cards");
            }
        });
        await Step("RelicCollection", async () =>
        {
            await Open<NCompendiumSubmenu>();
            await Open<NRelicCollection>();
        });
        await Step("PotionLab", async () =>
        {
            await Open<NCompendiumSubmenu>();
            await Open<NPotionLab>();
        });
        await Step("Bestiary", async () =>
        {
            await Open<NCompendiumSubmenu>();
            await Open<NBestiary>();
        });
        await Step("Statistics", async () =>
        {
            await Open<NCompendiumSubmenu>();
            await Open<NStatsScreen>();
        });
        await Step("RunHistory", async () =>
        {
            await Open<NCompendiumSubmenu>();
            await Open<NRunHistory>();
        });

        // --- Top-level screens -----------------------------------------------------
        await Step("Timeline", () => Open<NTimelineScreen>());
        await Step("Settings", () => Open<NSettingsScreen>());
        await Step("Profile", () => Open<NProfileScreen>());

        // --- Hover tips for every piece of Witch content ---------------------------
        // Exercises title/description formatting + every ExtraHoverTip (keywords, power previews,
        // FromCard<T> previews — the path that trips unregistered pools).
        System.Reflection.Assembly mod = typeof(WitchMenuTest).Assembly;
        foreach (CardModel card in ModelDb.AllCards.Where(c => c.GetType().Assembly == mod).OrderBy(c => c.GetType().Name))
        {
            await Step($"HoverTips/Card/{card.GetType().Name}", () => ShowTips(menu, [HoverTipFactory.FromCard(card), .. card.HoverTips]));
        }
        foreach (RelicModel relic in ModelDb.AllRelics.Where(r => r.GetType().Assembly == mod).OrderBy(r => r.GetType().Name))
        {
            await Step($"HoverTips/Relic/{relic.GetType().Name}", () => ShowTips(menu, relic.HoverTips));
        }
        foreach (PotionModel potion in ModelDb.AllPotions.Where(p => p.GetType().Assembly == mod).OrderBy(p => p.GetType().Name))
        {
            await Step($"HoverTips/Potion/{potion.GetType().Name}", () => ShowTips(menu, potion.HoverTips));
        }
        foreach (PowerModel power in ModelDb.AllPowers.Where(p => p.GetType().Assembly == mod).OrderBy(p => p.GetType().Name))
        {
            await Step($"HoverTips/Power/{power.GetType().Name}", () => ShowTips(menu, [HoverTipFactory.FromPower(power, 1)]));
        }

        if (failures.Count == 0)
        {
            AutoSlayLog.Action($"{Tag} all {total} screens/tips passed");
        }
        else
        {
            AutoSlayLog.Warn($"{Tag} {failures.Count}/{total} items failed:");
            foreach ((string item, Exception ex) in failures)
            {
                AutoSlayLog.Warn($"  - {item}: {ex.Message}");
            }
        }
        return (total, failures);
    }

    // FindChildren's type filter matches Godot native classes only, not C# script classes —
    // walk the tree and filter by CLR type instead.
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren(includeInternal: true))
        {
            if (child is T hit)
            {
                yield return hit;
            }
            foreach (T deeper in Descendants<T>(child))
            {
                yield return deeper;
            }
        }
    }

    private static Task ShowTips(Control owner, IEnumerable<IHoverTip> tips)
    {
        List<IHoverTip> list = tips.ToList();
        NHoverTipSet.Clear();
        NHoverTipSet? set = NHoverTipSet.CreateAndShow(owner, list);
        if (set == null && list.Count > 0)
        {
            throw new InvalidOperationException("NHoverTipSet.CreateAndShow returned null (hover tips blocked?)");
        }
        return Task.CompletedTask;
    }
}
