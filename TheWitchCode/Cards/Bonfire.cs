using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

public sealed class Bonfire : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromPower<BonfirePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BonfirePower>(3m)
    ];

    public Bonfire()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

        // Amount is the Bramble PRICE, not a stack count — re-applying must never add. Keep the cheapest price.
        decimal price = DynamicVars["BonfirePower"].BaseValue;
        if (Owner.Creature.GetPower<BonfirePower>() is { } existing)
        {
            if (existing.Amount > price)
            {
                await PowerCmd.ModifyAmount(choiceContext, existing, price - existing.Amount, Owner.Creature, this);
            }
        }
        else
        {
            await PowerCmd.Apply<BonfirePower>(choiceContext, Owner.Creature, price, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["BonfirePower"].UpgradeValueBy(-1m);
}
