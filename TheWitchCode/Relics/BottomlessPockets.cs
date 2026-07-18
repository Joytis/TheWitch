using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Bottomless Pockets: the Orobas upgrade of Large Pockets (see <see cref="TouchOfOrobasWitchPatch"/>).
/// Total potion-slot bonus becomes 3. Large Pockets' +1 is never revoked on removal (revoking would shrink
/// the belt and strand potions — the PotionBeltShrinkPatch trap), so on pickup this grants the delta.
/// </summary>
public sealed class BottomlessPockets : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PotionSlots", 3m)
    ];

    public override async Task AfterObtained()
    {
        int kept = ModelDb.Relic<LargePockets>().DynamicVars["PotionSlots"].IntValue;
        await PlayerCmd.GainMaxPotionCount(DynamicVars["PotionSlots"].IntValue - kept, Owner);
    }
}
