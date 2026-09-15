using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Potions;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Base for the Witch's payload-only potions: <see cref="PotionRarity.Token" /> and bound to
/// <see cref="WitchTokenPotionPool" /> instead of the character pool, so no event or random roll can ever
/// hand one out (see the pool's remarks). Grant them with <c>PotionCmd.TryToProcure&lt;T&gt;()</c>.
/// </summary>
[Pool(typeof(WitchTokenPotionPool))]
public abstract class WitchTokenPotion : WitchPotion
{
    public sealed override PotionRarity Rarity => PotionRarity.Token;
}
