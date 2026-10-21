using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Sack of Treats: your familiars create ALL of their cards at the start of turn — loot-table familiars
/// produce one of EACH entry instead of a single roll. Passive: <see cref="Powers.FamiliarPower" />
/// checks for this relic in its turn-start creation (and flashes it when it fires).
/// </summary>
public sealed class SackOfTreats : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        Familiars.HoverTip,
    ];
}
