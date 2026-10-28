using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// A Little Sip: a Power — whenever you use a potion, draw a card. Each copy adds one stack (one more card
/// per potion). Ethereal until upgraded. See <see cref="ALittleSipPower" />.
/// </summary>
public sealed class ALittleSip : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<ALittleSipPower>(1m)
    ];

    public ALittleSip()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ALittleSipPower>(choiceContext, Owner.Creature, DynamicVars["ALittleSipPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
