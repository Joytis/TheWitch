using System;
using System.Linq;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Moonbeam: strike, then bathe the target in <see cref="MoonlightPower" /> equal to the damage dealt (Blight Strike
/// pattern: blocked + unblocked, so Strength/Vigor scale the Moonlight too). It takes that much damage at the start
/// of each of your turns (stacks on repeat plays).
/// </summary>
public sealed class Moonbeam : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MoonlightPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
    ];

    public Moonbeam()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode((Creature c) => WitchFx.MoonbeamNode(Owner.Creature, c))
            // Cast sound on the attacker, impact thud on the hit — the Sovereign Blade split.
            .WithHitFx(null, WitchFx.CelestialSfx, null)
            .Execute(choiceContext);

        int dealt = attack.Results.SelectMany(r => r).Sum(r => r.TotalDamage);
        if (cardPlay.Target.IsAlive && dealt > 0)
        {
            await PowerCmd.Apply<MoonlightPower>(choiceContext, cardPlay.Target, dealt, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
