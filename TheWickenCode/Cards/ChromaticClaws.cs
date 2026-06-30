using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Chromatic Claws: an Attack that swaps a random belt potion for a fresh random one.</summary>
public sealed class ChromaticClaws : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8m, ValueProp.Move)
    ];

    public ChromaticClaws()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var rng = Owner.RunState.Rng.CombatPotionGeneration;
        List<PotionModel> belt = Owner.Potions.ToList();
        if (belt.Count == 0)
        {
            return;
        }

        PotionModel toDiscard = rng.NextItem(belt)!;
        await PotionCmd.Discard(toDiscard);

        PotionModel? created = PotionCatalog.Random(PotionCatalog.Query(), rng);
        if (created != null)
        {
            await PotionCmd.TryToProcure(created.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
