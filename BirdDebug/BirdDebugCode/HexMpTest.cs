using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace Bird.BirdDebug;

/// <summary>
/// --bird-hextest: Witch Hex trigger rules across party compositions (1-4 players, same-process test
/// combats — the hook pipeline is what lockstep MP runs on every client). Each check is a fresh
/// CULTISTS_NORMAL combat: the attacker plays its starting Attack once without Hex (baseline), then the
/// listed players apply 4 Hex, then the attacker plays the Attack again. Triggered = +8 damage over the
/// baseline AND Hex 4 → 3; not triggered = same damage AND Hex stays 4.
/// Rule under test: on an enemy, Hex responds to Witch attackers + any player who applied it.
/// Duck-typed (character + power looked up by type name) so BirdDebug references no character assembly.
/// </summary>
public static class HexMpTest
{
    public const string Tag = "[bird-hextest]";

    private const decimal HexApplied = 4m;
    private const decimal BonusPerHex = 2m;

    private sealed record Check(string Name, string[] Party, int[] Appliers, int Attacker, bool ExpectTrigger);

    private static readonly string[] Mixed = ["Witch", "Silent", "Ironclad", "Defect"];
    private static readonly string[] NoWitch = ["Silent", "Ironclad", "Defect", "Necrobinder"];
    private static readonly string[] AllWitch = ["Witch", "Witch", "Witch", "Witch"];
    private static readonly string[] TwoWitch = ["Witch", "Witch", "Silent", "Regent"];

    private static readonly Check[] Checks =
    [
        // The bug report: Silent with a Kaleidoscope Witch card, solo.
        new("Solo_Silent_OwnHex", ["Silent"], [0], 0, true),
        new("Solo_Witch_OwnHex", ["Witch"], [0], 0, true),
        new("Solo_Ironclad_OwnHex", ["Ironclad"], [0], 0, true),

        new("Mixed_WitchHex_WitchAttacks", Mixed, [0], 0, true),
        new("Mixed_WitchHex_SilentAttacks", Mixed, [0], 1, false),
        new("Mixed_WitchHex_IroncladAttacks", Mixed, [0], 2, false),
        new("Mixed_SilentHex_SilentAttacks", Mixed, [1], 1, true),
        new("Mixed_SilentHex_IroncladAttacks", Mixed, [1], 2, false),
        new("Mixed_SilentHex_WitchAttacks", Mixed, [1], 0, true),
        new("Mixed_MergedHex_SilentAttacks", Mixed, [0, 1], 1, true),
        new("Mixed_MergedHex_DefectAttacks", Mixed, [0, 1], 3, false),

        new("NoWitch_SilentHex_SilentAttacks", NoWitch, [0], 0, true),
        new("NoWitch_SilentHex_NecrobinderAttacks", NoWitch, [0], 3, false),
        new("NoWitch_SilentIroncladHex_IroncladAttacks", NoWitch, [0, 1], 1, true),

        new("AllWitch_W1Hex_W4Attacks", AllWitch, [0], 3, true),

        new("TwoWitch_W1Hex_W2Attacks", TwoWitch, [0], 1, true),
        new("TwoWitch_W1Hex_RegentAttacks", TwoWitch, [0], 3, false),
        new("TwoWitch_RegentHex_RegentAttacks", TwoWitch, [3], 3, true),
        new("TwoWitch_RegentHex_SilentAttacks", TwoWitch, [3], 2, false),
    ];

    public static async Task RunAll(string seed)
    {
        bool wasTestMode = TestMode.IsOn;
        TestMode.IsOn = true;
        List<(string item, Exception ex)> failures = [];
        int run = 0;
        try
        {
            PowerModel hex = ModelDb.AllPowers.Single(p => p.GetType().FullName == "TheWitch.TheWitchCode.Powers.HexPower");
            AutoSlayLog.Action($"{Tag} {Checks.Length} checks, seed '{seed}'");
            foreach (Check check in Checks)
            {
                run++;
                AutoSlayLog.Info($"{Tag} {check.Name}");
                try
                {
                    await RunCheck(check, hex, seed);
                }
                catch (Exception e)
                {
                    Exception actual = e.InnerException ?? e;
                    failures.Add((check.Name, actual));
                    AutoSlayLog.Error($"{Tag} FAILED {check.Name}: {actual.Message}");
                }
                finally
                {
                    EndCombat();
                }
            }
        }
        finally
        {
            TestMode.IsOn = wasTestMode;
            if (failures.Count == 0 && run == Checks.Length)
            {
                AutoSlayLog.Action($"{Tag} all {run} items passed");
            }
            else
            {
                AutoSlayLog.Warn($"{Tag} {failures.Count}/{run} items failed ({Checks.Length} defined):");
                foreach ((string item, Exception ex) in failures)
                {
                    AutoSlayLog.Warn($"  - {item}: {ex.Message}");
                }
            }
            NGame.Instance?.GetTree().Quit(failures.Count == 0 && run == Checks.Length ? 0 : 1);
        }
    }

    private static async Task RunCheck(Check check, PowerModel hexModel, string seed)
    {
        (CombatState combat, IReadOnlyList<Player> players) = await NewCombat(check.Party, seed);
        BlockingPlayerChoiceContext ctx = new();
        Creature enemy = combat.HittableEnemies.First();
        enemy.SetMaxHpInternal(9999m);
        enemy.SetCurrentHpInternal(9999m);
        Player attacker = players[check.Attacker];

        int baseline = await Strike(ctx, combat, attacker, enemy);

        foreach (int i in check.Appliers)
        {
            await PowerCmd.Apply(ctx, hexModel.ToMutable(), enemy, HexApplied, players[i].Creature, null);
        }
        decimal hexBefore = enemy.GetPower(hexModel.Id)?.Amount ?? 0m;
        decimal expectedBefore = HexApplied * check.Appliers.Length;
        if (hexBefore != expectedBefore)
        {
            throw new InvalidOperationException($"Hex after apply = {hexBefore}, expected {expectedBefore}");
        }

        int hexed = await Strike(ctx, combat, attacker, enemy);
        decimal hexAfter = enemy.GetPower(hexModel.Id)?.Amount ?? 0m;

        decimal expectedDamage = baseline + (check.ExpectTrigger ? BonusPerHex * hexBefore : 0m);
        decimal expectedHex = check.ExpectTrigger ? hexBefore - 1 : hexBefore;
        string observed = $"party [{string.Join(",", check.Party)}], appliers [{string.Join(",", check.Appliers.Select(i => check.Party[i]))}], " +
                          $"attacker {check.Party[check.Attacker]}: baseline {baseline}, hexed {hexed} (want {expectedDamage}), Hex {hexBefore}->{hexAfter} (want {expectedHex})";
        if (hexed != expectedDamage || hexAfter != expectedHex)
        {
            throw new InvalidOperationException(observed);
        }
        AutoSlayLog.Info($"  ok: {observed}");
    }

    /// <summary>Plays the attacker's starting Attack at the enemy; returns HP lost.</summary>
    private static async Task<int> Strike(PlayerChoiceContext ctx, CombatState combat, Player attacker, Creature enemy)
    {
        CardModel strike = combat.CreateCard(attacker.Character.StartingDeck.First(c => c.Type == CardType.Attack), attacker);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, attacker);
        int before = enemy.CurrentHp;
        await CardCmd.AutoPlay(ctx, strike, enemy);
        return before - enemy.CurrentHp;
    }

    private static async Task<(CombatState, IReadOnlyList<Player>)> NewCombat(string[] party, string seed)
    {
        if (CombatManager.Instance.DebugOnlyGetState() != null)
        {
            CombatManager.Instance.Reset(true);
        }

        List<Player> created = party
            .Select((name, i) => Player.CreateForNewRun(
                CharacterByName(name), UnlockState.all, (ulong)(i + 1)))
            .ToList();
        RunState run = RunState.CreateForTest(players: created, seed: seed);
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = RunManager.Instance.NetService.NetId;
        IReadOnlyList<Player> players = run.Players;

        ModelId encounterId = new(ModelId.SlugifyCategory<EncounterModel>(), "CULTISTS_NORMAL");
        EncounterModel encounter = ModelDb.GetById<EncounterModel>(encounterId).ToMutable();
        encounter.DebugRandomizeRng();
        CombatState combat = new(encounter, run, run.Modifiers, run.BadgeModels, run.RunModels);
        foreach (Player p in players)
        {
            combat.AddPlayer(p);
        }
        if (!encounter.HaveMonstersBeenGenerated)
        {
            encounter.GenerateMonstersWithSlots(run);
        }
        foreach ((MonsterModel monster, string? slot) in encounter.MonstersWithSlots)
        {
            combat.AddCreature(combat.CreateCreature(monster, CombatSide.Enemy, slot));
        }
        combat.SortEnemiesBySlotName();
        CombatManager.Instance.SetUpCombat(combat);
        CombatManager.Instance.AfterCombatRoomLoaded();

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        while (!CombatManager.Instance.IsInProgress && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Yield();
        }
        if (!CombatManager.Instance.IsInProgress)
        {
            throw new InvalidOperationException("combat never reached IsInProgress");
        }
        while (players.Any(p => p.PlayerCombatState?.Phase != PlayerTurnPhase.Play) && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Yield();
        }
        return (combat, players);
    }

    // Other installed mods may reuse class names: Witch from TheWitch, everything else base game.
    private static CharacterModel CharacterByName(string name)
    {
        string ns = name == "Witch" ? "TheWitch.TheWitchCode.Character" : "MegaCrit.Sts2.Core.Models.Characters";
        return ModelDb.AllCharacters.Single(c => c.GetType().FullName == $"{ns}.{name}");
    }

    private static void EndCombat()
    {
        try { CombatManager.Instance.Reset(true); } catch { /* best effort */ }
        try
        {
            AccessTools.PropertySetter(typeof(RunManager), "State").Invoke(RunManager.Instance, [null]);
            LocalContext.NetId = null;
        }
        catch { /* best effort */ }
    }
}
