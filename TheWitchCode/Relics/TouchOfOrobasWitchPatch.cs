using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// The Orobas boss event upgrades the starting relic via <see cref="TouchOfOrobas.GetUpgradedStarterRelic"/>,
/// whose hardcoded map falls back to Circlet for unknown starters. Route Large Pockets to Bottomless Pockets.
/// (Both the event's hover preview via SetupForPlayer and the actual AfterObtained replacement go through here.)
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasWitchPatch
{
    public static void Postfix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic is LargePockets)
        {
            __result = ModelDb.Relic<BottomlessPockets>().ToMutable();
        }
    }
}
