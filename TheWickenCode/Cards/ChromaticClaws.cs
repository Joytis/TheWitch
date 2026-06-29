using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Chromatic Claws: an Attack that scales with how many potions are in your belt right now. Built on the
/// <see cref="CalculatedDamageVar" /> (Soul Storm) pattern so the live total — ExtraDamage × belt potions —
/// renders correctly on the card face instead of mutating BaseValue.
/// </summary>
public sealed class ChromaticClaws : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(8m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((card, _) => card.Owner?.PotionSlots.Count(p => p != null) ?? 0),
    ];

    public ChromaticClaws()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(4m);
}
