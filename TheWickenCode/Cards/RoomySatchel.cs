using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Roomy Satchel: gain potion slots for this combat only. We grant the slots now
/// (<see cref="PlayerCmd.GainMaxPotionCount" />) and apply <see cref="RoomySatchelPower" /> to remember how many;
/// that power reverts them at combat end, so the bonus capacity — and any potion overflowing it — is lost.
/// </summary>
public sealed class RoomySatchel : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Slots", 2m)
    ];

    public RoomySatchel()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        int slots = DynamicVars["Slots"].IntValue;
        await PlayerCmd.GainMaxPotionCount(slots, Owner);
        await PowerCmd.Apply<RoomySatchelPower>(choiceContext, Owner.Creature, slots, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Slots"].UpgradeValueBy(1m);
}
