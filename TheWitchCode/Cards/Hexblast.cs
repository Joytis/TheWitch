using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Hexblast: lay 3 Hex on everyone, then detonate — damage scales with each enemy's Hex.</summary>
public sealed class Hexblast : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(3m),
        new DamageVar(10m, ValueProp.Move)
    ];

    public Hexblast()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HexPower>(choiceContext, CombatState!.HittableEnemies, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);

        foreach (Creature enemy in CombatState!.HittableEnemies.Where(e => e.IsAlive).ToList())
        {
            int hexes = enemy.GetPowerAmount<HexPower>();
            if (hexes <= 0)
            {
                continue;
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue * hexes)
                .FromCard(this)
                .Targeting(enemy)
                // Purple occult flame under the target (preloaded via Witch.ExtraAssetPaths) + heavy sting.
                .WithHitVfxNode(t => NGroundFireVfx.Create(t, VfxColor.Purple))
                .WithHitFx(null, null, "heavy_attack.mp3")
                .Execute(choiceContext);
        }

        // Detonation punch on top of the damage-scaled hit shake.
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
