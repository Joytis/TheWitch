using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Bear Cubs: a plain counter (NOT a <see cref="FamiliarPower" /> — cubs make no cards and don't count as familiars).
/// Granted by Hibernate (via <see cref="BearCubNextTurnPower" />), read by Forage. One tiny cosmetic
/// <see cref="BearCubPet" /> per stack.
/// </summary>
public sealed class BearCubPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private static WitchPet Pet => ModelDb.Monster<BearCubPet>();

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            await WitchPetSync.Sync(this, Pet, Amount);
        }
    }

    public override Task AfterRemoved(Creature oldOwner) => WitchPetSync.RemoveAll(oldOwner, Pet);
}
