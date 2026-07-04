using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Patches;

/// <summary>
/// Registers the Wicken's starter-transcendence pair in Archaic Tooth's map (base game: Bash→Break, etc.):
/// <see cref="Brew" /> → <see cref="Infuse" />. The property getter rebuilds the dictionary on every
/// access, so a postfix covers all readers — including <c>ArchaicTooth.TranscendenceCards</c>, which
/// DustyTome uses to EXCLUDE transcendence cards from its unique-ancient roll.
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
public static class AncientTranscendencePatch
{
    private static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<Brew>().Id] = ModelDb.Card<Infuse>();
    }
}
