using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using TheAugur.TheAugurCode.Cards;
using TheAugur.TheAugurCode.Relics;

namespace TheAugur.TheAugurCode.Character;

/// <summary>
/// The Augur: time-bending archetypes. Suspend (cards set aside to play on a later turn), Paradox
/// (Split cards break into paired clones that detonate when reunited in hand), and deck manipulation.
/// Prototype: every visual rides the placeholder (Silent) assets until the character gets art.
/// </summary>
public class Augur : PlaceholderCharacterModel
{
    public const string CharacterId = "Augur";

    // Placeholder base-game visuals/audio (creature, energy counter, rest site, hands, sfx, ...).
    public override string PlaceholderID => "silent";

    // Colors
    public static readonly Color Color = new("2B4C6F");
    public static readonly Color DarkColor = new("16283BFF");
    public override Color NameColor => Color;
    public override Color MapDrawingColor => new("2E4E70");
    public override Color RemoteTargetingLineColor => Color;
    public override Color RemoteTargetingLineOutline => DarkColor;
    public override Color EnergyLabelOutlineColor => DarkColor;

    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeAugur>(),
        ModelDb.Card<StrikeAugur>(),
        ModelDb.Card<StrikeAugur>(),
        ModelDb.Card<StrikeAugur>(),
        ModelDb.Card<DefendAugur>(),
        ModelDb.Card<DefendAugur>(),
        ModelDb.Card<DefendAugur>(),
        ModelDb.Card<DefendAugur>(),
        ModelDb.Card<AugurStarterA>(),
        ModelDb.Card<AugurStarterB>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<AugurStarterRelic>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AugurCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AugurRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AugurPotionPool>();
}
