using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Pact of Agony: hex yourself to charge your potions — they hit far harder for the rest of the turn.</summary>
public sealed class PactOfAgony : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m),
        new DamageVar(10m, ValueProp.Move),
        // Percent additional potion damage (Lethality model): +100% = double, upgrading to +200% = triple.
        new PowerVar<PotionDamageMultiplierPower>(100m)
    ];

    public PactOfAgony()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await PowerCmd.Apply<HexPower>(choiceContext, Owner.Creature, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath, null, "heavy_attack.mp3")
            .Execute(choiceContext);

        await PowerCmd.Apply<PotionDamageMultiplierPower>(
            choiceContext, Owner.Creature, DynamicVars["PotionDamageMultiplierPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["PotionDamageMultiplierPower"].UpgradeValueBy(100m);
}
