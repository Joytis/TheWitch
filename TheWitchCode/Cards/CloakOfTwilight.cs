using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Cloak of Twilight: whenever you trigger Hex, gain Block (<see cref="CloakOfTwilightPower" />).</summary>
public sealed class CloakOfTwilight : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<CloakOfTwilightPower>(2m)
    ];

    public CloakOfTwilight()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CloakOfTwilightPower>(
            choiceContext, Owner.Creature, DynamicVars["CloakOfTwilightPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["CloakOfTwilightPower"].UpgradeValueBy(1m);
}
