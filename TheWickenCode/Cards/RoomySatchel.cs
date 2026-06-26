using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Roomy Satchel: gain potion slots via <see cref="PlayerCmd.GainMaxPotionCount" />. NOTE: this raises the
/// run-level max potion count (persists beyond combat). If repeatable stacking is undesired, scope it to
/// combat (revert on combat end) — see plan doc.
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
        await PlayerCmd.GainMaxPotionCount(DynamicVars["Slots"].IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Slots"].UpgradeValueBy(1m);
}
