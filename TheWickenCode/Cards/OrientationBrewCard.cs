using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Extensions;
using TheWicken.TheWickenCode.Potions.Brewing;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Shared base for the "brew a potion of orientation X" trio (Wicked / Stony / Herbal Brew). Owns the one
/// canonical brew sequence — rarity buffs consumed in order (next-is-Rare, then next-is-upgraded), then a
/// random catalog roll at the resulting rarity. Any change to buff precedence or the roll happens here only.
/// </summary>
public abstract class OrientationBrewCard : WickenCard
{
    protected abstract PotionOrientation Orientation { get; }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected OrientationBrewCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var rarity = await NextPotionRarePower.MakeNextRare(Owner, PotionRarity.Common);
        rarity = await NextPotionUpgradedPower.UpgradeRarity(Owner, rarity);
        var potion = PotionCatalog.Random(
            PotionCatalog.Query(orientation: Orientation, rarity: rarity),
            Owner.RunState.Rng.CombatPotionGeneration);

        if (potion != null)
        {
            // Orientation-coded splash layered under the generic brew puff: red = offensive,
            // blue = defensive, green = utility.
            WickenFx.Splash(Owner.Creature, Orientation switch
            {
                PotionOrientation.Offensive => new Godot.Color("d04545"),
                PotionOrientation.Defensive => new Godot.Color("4a7bd0"),
                _ => WickenFx.WickenGreen,
            });
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
