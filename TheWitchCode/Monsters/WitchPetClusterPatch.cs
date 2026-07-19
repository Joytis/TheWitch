using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Places familiar pets at their species' spawn marker in the witch's visuals scene
/// ("bear_familiar_pos", ... — falls back to FamiliarPos, then to the game's default pet layout).
///
/// TWO game paths lay pets out in a line and must both be post-fixed:
/// <see cref="NCombatRoom.PositionPlayersAndPets" /> (combat start) and
/// <see cref="NCombatRoom.AddCreature" /> (mid-combat pet spawn — it re-lines ALL of the
/// owner's pets every time any pet is added, stomping marker positions).
///
/// Multiples of one species stack in a pyramid (1 / 2 3 / 4 5 6 ...) below-and-outward from
/// the marker, spaced by the species' <see cref="PetConfig.SpeciesDistance" />.
/// </summary>
[HarmonyPatch]
public static class WitchPetClusterPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
    private static void AfterLayout(List<NCreature> creatureNodes)
    {
        PositionWitchPets(creatureNodes);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
    private static void AfterAddCreature(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature, List<NCreature> ____creatureNodes)
    {
        if (creature.PetOwner != null)
        {
            PositionWitchPets(____creatureNodes);
        }
    }

    private static void PositionWitchPets(List<NCreature> creatureNodes)
    {
        List<NCreature> players = creatureNodes.Where(c => c.Entity?.IsPlayer == true).ToList();

        var petsByOwner = creatureNodes
            .Where(c => c.Entity?.Monster is WitchPet)
            .GroupBy(c => c.Entity.PetOwner);

        foreach (var ownerGroup in petsByOwner)
        {
            NCreature? owner = players.FirstOrDefault(p => p.Entity.Player == ownerGroup.Key);
            if (owner?.Visuals == null)
            {
                continue;
            }

            // GroupBy preserves first-appearance order and creatureNodes is append-only, so
            // species blocks and the pets within them are stable across calls — no reshuffling.
            List<NCreature> allPets = [];
            foreach (var speciesGroup in ownerGroup.GroupBy(c => c.Entity.Monster!.GetType()))
            {
                List<NCreature> pets = speciesGroup.ToList(); // list order = spawn order = stack order
                WitchPet species = (WitchPet)pets[0].Entity.Monster!;

                // Marker node named after the pet file: "bear_familiar" → "bear_familiar_pos".
                Node2D? marker = owner.Visuals.FindChild(species.PetFileName + "_pos", recursive: true, owned: false) as Node2D
                    ?? owner.Visuals.FindChild("FamiliarPos", recursive: true, owned: false) as Node2D;
                if (marker == null)
                {
                    continue; // no markers in this character's scene — keep the game's default layout
                }

                float distance = 40f;
                if (ResourceLoader.Exists(species.ConfigPath) && ResourceLoader.Load(species.ConfigPath) is PetConfig cfg)
                {
                    distance = cfg.SpeciesDistance;
                }

                for (int i = 0; i < pets.Count; i++)
                {
                    pets[i].GlobalPosition = marker.GlobalPosition + SpeciesOffset(i, distance);
                }

                allPets.AddRange(pets);
            }

            // Depth-sort: pets higher on screen (smaller Y) draw further back. OrderBy is stable,
            // so equal-Y pets keep their species/pyramid order.
            allPets = allPets.OrderBy(p => p.GlobalPosition.Y).ToList();

            // ONE deterministic draw-order pass: all pets render BEHIND the witch (never over her
            // or her UI), depth-sorted as above — the game's AddCreature inserts every new pet at
            // player+1, which both shuffles existing pets around and draws them over the player.
            //
            // MoveChild targets are FINAL indices (node removed, then reinserted), so nudging pets
            // relative to owner.GetIndex() flip-flops nodes that are already on the correct side.
            // Instead: build the entire desired sibling order, then enforce it left-to-right —
            // stable, idempotent, and independent of the pets' current positions.
            Node parent = owner.GetParent();
            List<Node> desired = parent.GetChildren().ToList();
            desired.RemoveAll(n => allPets.Contains(n));
            desired.InsertRange(desired.IndexOf(owner), allPets);
            for (int i = 0; i < desired.Count; i++)
            {
                parent.MoveChildSafely(desired[i], i);
            }
        }
    }

    /// <summary>
    /// Pyramid layout for the n-th pet of one species: row r holds r+1 pets, centered under the
    /// marker — index 0 on top, then (1 2), then (3 4 5), ... Rows step down by half the spacing.
    /// </summary>
    private static Vector2 SpeciesOffset(int index, float distance)
    {
        int row = 0;
        int firstInRow = 0;
        while (index >= firstInRow + row + 1)
        {
            firstInRow += row + 1;
            row++;
        }

        int column = index - firstInRow;
        return new Vector2((column - row * 0.5f) * distance, row * distance * 0.5f);
    }
}
