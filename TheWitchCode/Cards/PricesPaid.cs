using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Prices Paid: bleed yourself for a strike, then brew the spilled blood into a Noxious Brew.</summary>
public sealed class PricesPaid : WitchCard
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<NoxiousBrew>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move),
    ];

    public PricesPaid()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Lose HP.
        int times = ResolveEnergyXValue();
        await CreatureCmd.Damage(choiceContext, Owner.Creature, times, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, "vfx/vfx_bloody_impact");
        SfxCmd.Play("slash_attack.mp3");

        // make brews
        if (IsUpgraded)
		{
			times++;
		}
        await WitchCmd.TryProcureNoxiousBrews(Owner, times);

        // Deal Damate
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
    }
}
