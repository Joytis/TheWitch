using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Hovering a cosmetic familiar pet shows the same tooltip as its <see cref="Powers.FamiliarPower" />
/// stack on the player. Pets carry no powers of their own, so <see cref="Creature.HoverTips" />
/// is empty for them; this postfix appends the source power's tips (the getter isn't virtual,
/// hence the Harmony patch). Read-only + per-client cosmetic — no MP sync concerns.
/// </summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.HoverTips), MethodType.Getter)]
public static class WitchPetHoverTipsPatch
{
    private static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance.Monster is WitchPet { SourcePower: { } power })
        {
            __result = __result.Concat(power.HoverTips).ToList();
        }
    }
}
