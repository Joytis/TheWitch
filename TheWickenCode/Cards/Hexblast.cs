using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Hexblast: lay on Hex, then hit harder the more debuffs the target is already suffering.</summary>
public sealed class Hexblast : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<HexPower>(3m)
    ];

    public Hexblast()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // Apply Hex first so it always counts toward the unique-debuff tally (guarantees at least one).
        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);

        int debuffs = cardPlay.Target.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .Select(p => p.GetType())
            .Distinct()
            .Count();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue * debuffs)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
