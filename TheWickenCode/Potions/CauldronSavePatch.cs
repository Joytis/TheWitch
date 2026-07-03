using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// Persists The Cauldron's poured state across save/quit/resume. The game saves potions as id + slot only
/// (<c>SerializablePotion</c>) and <c>ExtraPlayerFields</c> is a fixed schema, so there is no in-save
/// extension point — instead a small sidecar JSON lives in the Godot user dir mapping
/// (player seed, slot index) → cauldron stats.
///
/// Write side: postfix on <c>Player.ToSerializable</c> — fires on every run-save snapshot; rewrites (or
/// deletes) the sidecar from the live belt. Read side: postfix on the private <c>Player.LoadPotions</c> —
/// runs inside <c>Player.FromSerializable</c>, after <c>PlayerRng</c> is restored (so the seed key is
/// valid) and after the Cauldron instances exist in the belt. A seed mismatch (new run, stale file) means
/// the entry is ignored, so the failure mode is only ever an empty Cauldron.
///
/// Not MP-synced: remote clients rebuild potions from packets, not from this file.
/// </summary>
public static class CauldronSaveState
{
    private sealed class SlotState
    {
        public decimal Strength { get; set; }
        public decimal Heal { get; set; }
        public decimal Energy { get; set; }
        public decimal Cleanse { get; set; }
        public decimal Intangible { get; set; }
    }

    private sealed class SaveFile
    {
        public uint Seed { get; set; }
        public Dictionary<int, SlotState> Slots { get; set; } = [];
    }

    private static string FilePath =>
        Path.Combine(Godot.ProjectSettings.GlobalizePath("user://"), "thewicken_cauldron_state.json");

    public static void Write(Player player)
    {
        try
        {
            var slots = new Dictionary<int, SlotState>();
            for (int i = 0; i < player.PotionSlots.Count; i++)
            {
                if (player.PotionSlots[i] is TheCauldron cauldron &&
                    cauldron.DynamicVars["StrengthPower"].BaseValue + cauldron.DynamicVars["Heal"].BaseValue > 0)
                {
                    slots[i] = new SlotState
                    {
                        Strength = cauldron.DynamicVars["StrengthPower"].BaseValue,
                        Heal = cauldron.DynamicVars["Heal"].BaseValue,
                        Energy = cauldron.DynamicVars["Energy"].BaseValue,
                        Cleanse = cauldron.DynamicVars["Cleanse"].BaseValue,
                        Intangible = cauldron.DynamicVars["IntangiblePower"].BaseValue,
                    };
                }
            }

            if (slots.Count == 0)
            {
                File.Delete(FilePath);
                return;
            }

            var data = new SaveFile { Seed = player.PlayerRng.Seed, Slots = slots };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            Log.Warn($"[TheWicken] Failed to write Cauldron sidecar save: {ex.Message}");
        }
    }

    public static void Restore(Player player)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            SaveFile? data = JsonSerializer.Deserialize<SaveFile>(File.ReadAllText(FilePath));
            if (data == null || data.Seed != player.PlayerRng.Seed)
            {
                return; // stale file from an older run — ignore.
            }

            foreach ((int slot, SlotState state) in data.Slots)
            {
                if (slot < player.PotionSlots.Count && player.PotionSlots[slot] is TheCauldron cauldron)
                {
                    cauldron.RestoreState(state.Strength, state.Heal, state.Energy, state.Cleanse, state.Intangible);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[TheWicken] Failed to restore Cauldron sidecar save: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ToSerializable))]
public static class CauldronSaveWritePatch
{
    private static void Postfix(Player __instance) => CauldronSaveState.Write(__instance);
}

[HarmonyPatch(typeof(Player), "LoadPotions")]
public static class CauldronSaveRestorePatch
{
    private static void Postfix(Player __instance) => CauldronSaveState.Restore(__instance);
}
