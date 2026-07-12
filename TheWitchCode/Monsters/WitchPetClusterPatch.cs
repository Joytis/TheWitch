using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Clusters same-type familiar pets together on the board. The game lays pets out in the order they appear
/// in the node list (spawn order), spaced evenly across the player's width — so with one pet per familiar
/// stack (see <c>FamiliarPower.SyncPets</c>), summoning Wolf, Owl, Wolf would interleave the wolves. This
/// prefix stably regroups the <see cref="WitchPet" /> nodes by pet type before
/// <see cref="NCombatRoom.PositionPlayersAndPets" /> assigns positions: only witch-pet slots are rewritten
/// (players, Osty, and other base-game pets keep their exact positions in the list), group order follows
/// each type's first appearance, and within a group spawn order is preserved. Per-player pet lists are
/// subsequences of this list, so each player's pets stay grouped too.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class WitchPetClusterPatch
{
    private static void Prefix(List<NCreature> creatureNodes)
    {
        List<int> slots = [];
        for (int i = 0; i < creatureNodes.Count; i++)
        {
            if (creatureNodes[i].Entity?.Monster is WitchPet)
            {
                slots.Add(i);
            }
        }

        if (slots.Count < 2)
        {
            return;
        }

        List<NCreature> grouped = slots
            .Select(i => creatureNodes[i])
            .GroupBy(n => (n.Entity.PetOwner, n.Entity.Monster!.GetType()))
            .SelectMany(g => g)
            .ToList();
        for (int i = 0; i < slots.Count; i++)
        {
            creatureNodes[slots[i]] = grouped[i];
        }
    }
}
