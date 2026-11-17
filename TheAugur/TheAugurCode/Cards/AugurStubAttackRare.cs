using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAugur.TheAugurCode.Cards;

/// <summary>Reward/merchant-pool stub (Rare Attack). Card rewards and merchant stock throw ("couldn't generate a
/// valid rarity") unless every card type has cards at reachable rarities; these keep the bot and rewards
/// working until real cards exist. Replace with a designed card.</summary>
public sealed class AugurStubAttackRare : AugurCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(24m, ValueProp.Move)
    ];

    public AugurStubAttackRare()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.scratchPath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);
}
