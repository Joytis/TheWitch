using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Wolf familiar token. Escalates with the "pack": when this card is generated into combat it bakes in +5 damage
/// for each Gnash already played this combat, so every new Gnash hits harder than the last — and because the bonus
/// is baked into the card's <see cref="DamageVar" />, the growing number is visible on the card face. The Maul-style
/// "buff every copy on play" approach can't work here: Gnash Exhausts and the Wolf respawns a fresh one each turn,
/// so a play-time buff never carries across the exhaust/respawn cycle. Reading combat history at generation does.
/// </summary>
public sealed class Gnash : WickenFamiliarCard
{
    private const int PackBonusPerGnash = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move)
    ];

    public Gnash()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        // Only when THIS card is the one being generated: bake the pack bonus into its damage so the displayed
        // value already reflects every Gnash played so far this combat.
        if (card == this)
        {
            int priorGnash = CombatHistoryQueries.CardsPlayedThisCombat<Gnash>(Owner.Creature);
            if (priorGnash > 0)
            {
                DynamicVars.Damage.BaseValue += PackBonusPerGnash * priorGnash;
            }
        }

        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
