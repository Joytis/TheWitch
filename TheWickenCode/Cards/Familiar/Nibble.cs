using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Rat familiar token. Swarm payoff: hits once for every Rat card (Rats, Plague, Nibble) played this
/// combat, including itself — base 1 hit (this Nibble) + 1 hit per prior Rat card played this combat.
/// </summary>
public sealed class Nibble : WickenFamiliarCard, IRatCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1m, ValueProp.Move)
    ];

    public Nibble()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hits = 1 + (Owner?.Creature is { } creature ? CombatHistoryQueries.RatCardsPlayedThisCombat(creature) : 0);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
