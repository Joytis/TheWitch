using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Experiment (was Chromatic Claws): block up while swapping a random belt potion for a fresh random one.</summary>
public sealed class Experiment : WickenCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7m, ValueProp.Move)
    ];

    public Experiment()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

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

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
