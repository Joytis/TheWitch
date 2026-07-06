using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Hexblast: detonate every debuff on the target, dealing damage per debuff, then clearing them.</summary>
public sealed class Hexblast : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(20m, ValueProp.Move)
    ];

    public Hexblast()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        List<PowerModel> debuffs = cardPlay.Target.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .GroupBy(p => p.GetType())
            .Select(g => g.First())
            .ToList();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue * debuffs.Count)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
