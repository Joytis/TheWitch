using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Shared "one cosmetic pet per power stack" bookkeeping, used by <see cref="Powers.FamiliarPower" /> and any
/// other power that shows pets. Pets are matched to their power by pet TYPE, so each power type must
/// use its own <see cref="WitchPet" /> subclass.
/// </summary>
public static class WitchPetSync
{
    /// <summary>The owner's live pets of <paramref name="canonical" />'s type, in spawn (= stack) order.</summary>
    public static List<Creature> FindPets(Player player, WitchPet canonical) =>
        player.PlayerCombatState!.Pets.Where(p => p.Monster?.GetType() == canonical.GetType()).ToList();

    /// <summary>Spawn/kill pets of <paramref name="canonical" />'s type until the owner has exactly <paramref name="want" />.</summary>
    public static async Task Sync(PowerModel source, WitchPet canonical, int want)
    {
        Creature owner = source.Owner;
        if (owner.Player is not { PlayerCombatState: not null } player || owner.CombatState is not { } combat || owner.IsDead)
        {
            return;
        }

        List<Creature> pets = FindPets(player, canonical);
        want = Math.Max(0, want);
        for (int i = pets.Count; i < want; i++)
        {
            WitchPet petModel = (WitchPet)canonical.ToMutable();
            petModel.SourcePower = source;
            petModel.StackIndex = i;
            Creature pet = combat.CreateCreature(petModel, owner.Side, null);
            await PlayerCmd.AddPet(pet, player);
        }
        for (int i = pets.Count - 1; i >= want; i--)
        {
            await CreatureCmd.Kill(pets[i], force: true);
        }
    }

    /// <summary>Despawn every pet of <paramref name="canonical" />'s type when its power is removed outright.</summary>
    public static async Task RemoveAll(Creature oldOwner, WitchPet canonical)
    {
        if (oldOwner.Player is not { PlayerCombatState: not null } player)
        {
            return;
        }

        /* Owner death runs this from inside CreatureCmd.KillWithoutCheckingWinCondition's
           RemoveAllPowersAfterDeath loop. Killing a pet there removes its NCreature node, and any
           caller still iterating a snapshot that includes the pets then dereferences a null node —
           The Insatiable's SandpitPower.AfterRemoved does exactly that (player + Target.Pets array
           captured up front, Visuals.Visible set with no null check), so the NRE aborts the enemy
           turn and the surviving co-op player's turn never starts. Hide the pets instead; the
           combat's pet list is torn down with the combat anyway. */
        bool ownerDied = oldOwner.IsDead;

        foreach (Creature pet in FindPets(player, canonical))
        {
            if (ownerDied)
            {
                NCreature? petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
                if (petNode != null)
                {
                    petNode.Visuals.Visible = false;
                }
                continue;
            }

            await CreatureCmd.Kill(pet, force: true);
        }
    }
}
