using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Feast With Wolves: guard the kill and dig through your deck until an Attack turns up.</summary>
public sealed class FeastWithWolves : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move)
    ];

    public FeastWithWolves()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // Draw one at a time until an Attack surfaces; Draw returns null when nothing more can be drawn
        // (empty draw + discard, or a full hand) — that's the loop's safety exit.
        while (true)
        {
            CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
            if (drawn == null || drawn.Type == CardType.Attack)
            {
                break;
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
