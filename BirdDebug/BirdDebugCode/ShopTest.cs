using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace Bird.BirdDebug;

/// <summary>
/// --bird-shoptest: merchant-generation statistics for the selected character, per game mode, in a
/// throwaway test run (no scene, no combat). Built to check the Witch's MothShopThrottlePatch:
///   Standard  rolls the merchant's Power slot (CardFactory.CreateForMerchant, CardType.Power) N times
///             with the throttle on, then N times with every other mod's prefix on that method
///             unpatched (the unthrottled baseline), and reports how often a watched card (Moth
///             Familiar for the Witch) fills the slot. PASS = throttled rate within tolerance of the
///             patch's 10% target.
///   Blitz     builds full Blitz merchants and counts CreateForMerchant(type) calls — Blitz shops are
///             BlitzMerchantEntry choices, so the throttle should never run (PASS = 0 calls) — then
///             rolls the Blitz shop's "card rewards" entry (3-card rewards at Blitz odds, Shops rng)
///             N times and reports the watched card's appearance rate (informational).
/// Exit code 0 = all checks passed, 1 = a check failed.
/// </summary>
public static class ShopTest
{
    public const string Tag = "[bird-shoptest]";

    private const int Rolls = 20000;
    private const int BlitzMerchants = 200;
    private const float StandardTarget = 0.10f;
    private const float StandardTolerance = 0.025f;

    /// <summary>The card to watch, by class name, per character key.</summary>
    private static readonly Dictionary<string, string> Watched = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Witch"] = "MothFamiliar",
    };

    private static int s_powerSlotCalls;

    public static async Task RunAll(string seed)
    {
        List<string> failures = [];
        bool wasTestMode = TestMode.IsOn;
        TestMode.IsOn = true;
        Harmony harmony = new("bird.shoptest");
        MethodInfo powerSlot = AccessTools.Method(typeof(CardFactory), nameof(CardFactory.CreateForMerchant),
            [typeof(Player), typeof(IEnumerable<CardModel>), typeof(IEnumerable<CardModel>), typeof(CardType)]);
        try
        {
            harmony.Patch(powerSlot, prefix: new HarmonyMethod(typeof(ShopTest), nameof(CountPowerSlotPrefix)));

            string watchedName = Watched.TryGetValue(BirdCharacter.Key, out string? w) ? w : "";
            CardModel? watched = ModelDb.AllCards.FirstOrDefault(c => c.GetType().Name == watchedName);
            AutoSlayLog.Action($"{Tag} character={BirdCharacter.Key} watched={watched?.GetType().Name ?? "(none)"} rolls={Rolls} seed='{seed}'");

            // ---- Standard -------------------------------------------------------------------
            {
                Player player = NewRun(GameMode.Standard, seed);
                List<CardModel> pool = CharacterPool(player);
                int powers = pool.Count(c => c.Type == CardType.Power);
                AutoSlayLog.Info($"{Tag} Standard: pool {pool.Count} cards, {powers} Powers ({pool.Count(c => c.Type == CardType.Power && c.Rarity == CardRarity.Common)} Common)");

                s_powerSlotCalls = 0;
                MerchantInventory.CreateForNormalMerchant(player);
                AutoSlayLog.Info($"{Tag} Standard: one full merchant -> {s_powerSlotCalls} CreateForMerchant(type) calls (Attack/Skill/Power slots)");

                float throttled = PowerSlotRate(player, pool, watched);
                AutoSlayLog.Info($"{Tag} Standard: throttle ON  -> watched card in Power slot {throttled:P1} of {Rolls}");

                // Baseline: drop every non-Bird prefix on the Power-slot roll (the mod's throttle).
                Patches? info = Harmony.GetPatchInfo(powerSlot);
                List<string> owners = info?.Prefixes.Select(p => p.owner).Where(o => o != harmony.Id).Distinct().ToList() ?? [];
                foreach (string owner in owners)
                {
                    harmony.Unpatch(powerSlot, HarmonyPatchType.Prefix, owner);
                }
                float baseline = PowerSlotRate(player, pool, watched);
                AutoSlayLog.Info($"{Tag} Standard: throttle OFF -> watched card in Power slot {baseline:P1} of {Rolls} (unpatched owners: {string.Join(", ", owners)})");

                if (watched != null && Math.Abs(throttled - StandardTarget) > StandardTolerance)
                {
                    failures.Add($"Standard throttled rate {throttled:P1} is outside {StandardTarget:P0} ± {StandardTolerance:P1}");
                }
            }

            // ---- Blitz ----------------------------------------------------------------------
            ClearRun();
            {
                Player player = NewRun(GameMode.Blitz, seed);
                s_powerSlotCalls = 0;
                Dictionary<string, int> entryTypes = new();
                for (int i = 0; i < BlitzMerchants; i++)
                {
                    MerchantInventory inv = MerchantInventory.CreateForBlitzMerchant(player);
                    foreach (BlitzMerchantEntry e in inv.BlitzEntries)
                    {
                        string k = e.MerchantEntryType.ToString();
                        entryTypes[k] = entryTypes.GetValueOrDefault(k) + 1;
                    }
                }
                AutoSlayLog.Info($"{Tag} Blitz: {BlitzMerchants} merchants -> {s_powerSlotCalls} CreateForMerchant(type) calls; entries: {string.Join(", ", entryTypes.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"))}");
                if (s_powerSlotCalls != 0)
                {
                    failures.Add($"Blitz merchants rolled the typed card slot {s_powerSlotCalls} times (expected 0)");
                }

                if (watched != null)
                {
                    CardCreationOptions options = new CardCreationOptions([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.Blitz)
                        .WithRngOverride(player.PlayerRng.Shops);
                    int rewardsWithWatched = 0;
                    for (int i = 0; i < Rolls; i++)
                    {
                        if (CardFactory.CreateForReward(player, 3, options).Any(r => r.Card.CanonicalInstance.GetType() == watched.GetType()))
                        {
                            rewardsWithWatched++;
                        }
                    }
                    AutoSlayLog.Info($"{Tag} Blitz: card-rewards entry -> watched card in {(float)rewardsWithWatched / Rolls:P1} of {Rolls} 3-card rewards (entry offers 2 rewards)");
                }
            }
        }
        catch (Exception e)
        {
            failures.Add($"exception: {e}");
        }
        finally
        {
            ClearRun();
            harmony.UnpatchAll(harmony.Id);
            TestMode.IsOn = wasTestMode;
            if (failures.Count == 0)
            {
                AutoSlayLog.Action($"{Tag} all checks passed");
            }
            else
            {
                foreach (string f in failures)
                {
                    AutoSlayLog.Warn($"{Tag} FAILED {f}");
                }
            }
            NGame.Instance?.GetTree().Quit(failures.Count == 0 ? 0 : 1);
        }
        await Task.CompletedTask;
    }

    private static void CountPowerSlotPrefix(CardType type)
    {
        s_powerSlotCalls++;
    }

    /// <summary>RunManager.State is private-set; clear it so the next mode can SetUpTest again (CardTest.EndCombat shape).</summary>
    private static void ClearRun()
    {
        try
        {
            AccessTools.PropertySetter(typeof(RunManager), "State").Invoke(RunManager.Instance, [null]);
            LocalContext.NetId = null;
        }
        catch { /* best effort */ }
    }

    private static Player NewRun(GameMode mode, string seed)
    {
        Player player = Player.CreateForNewRun(BirdCharacter.Model, UnlockState.all, 1UL);
        RunState run = RunState.CreateForTest(players: [player], gameMode: mode, seed: seed);
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = RunManager.Instance.NetService.NetId;
        return run.Players[0];
    }

    private static List<CardModel> CharacterPool(Player player) =>
        player.Character.CardPool.GetUnlockedCards(player.UnlockState, ((IRunState)player.RunState).CardMultiplayerConstraint).ToList();

    private static float PowerSlotRate(Player player, List<CardModel> pool, CardModel? watched)
    {
        if (watched == null)
        {
            return 0f;
        }
        int hits = 0;
        for (int i = 0; i < Rolls; i++)
        {
            CardCreationResult r = CardFactory.CreateForMerchant(player, pool, Array.Empty<CardModel>(), CardType.Power);
            if (r.Card.CanonicalInstance.GetType() == watched.GetType())
            {
                hits++;
            }
        }
        return (float)hits / Rolls;
    }
}
