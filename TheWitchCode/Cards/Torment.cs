using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Marker for attacks that don't burn the target's Hex — <see cref="HexPower" /> skips its
/// after-attack stack loss when the attacking card implements this.
/// </summary>
public interface IHexPreserving;

/// <summary>
/// Torment: a heavy hit that turns every potion you play this turn into a random-enemy Hex
/// (via <see cref="TormentPower" />, one stack per play, gone at end of turn).
/// </summary>
public sealed class Torment : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<TormentPower>(1m)
    ];

    public Torment()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<TormentPower>(choiceContext, Owner.Creature, DynamicVars[nameof(TormentPower)].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
