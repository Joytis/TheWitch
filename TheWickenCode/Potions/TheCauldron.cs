using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// The Cauldron: card-only accumulator potion (created/filled by Cackle). Starts empty; Cackle pours the
/// rest of the belt into it — each poured potion adds Strength + healing, with bonus effects at 2/3/4+
/// poured in one cast (Energy / remove a debuff / Intangible). Token rarity so it never rolls as a drop.
/// State lives in this mutable instance's DynamicVars — potions serialize as id+slot only
/// (SerializablePotion), so poured effects DO NOT survive save/quit/resume; the Cauldron reverts to empty.
/// </summary>
public sealed class TheCauldron : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(0m),
        new DynamicVar("Heal", 0m),
        new DynamicVar("Energy", 0m),
        new DynamicVar("Cleanse", 0m),
        new PowerVar<IntangiblePower>(0m)
    ];

    /// <summary>
    /// Pour <paramref name="count" /> discarded potions in (one Cackle cast). Strength/healing accumulate
    /// per potion; the threshold effects (2+: Energy, 3+: cleanse, 4+: Intangible) are unlocked, not stacked.
    /// </summary>
    public void PourPotions(int count)
    {
        AssertMutable();
        DynamicVars["StrengthPower"].BaseValue += 2m * count;
        DynamicVars["Heal"].BaseValue += 3m * count;
        if (count >= 2)
        {
            DynamicVars["Energy"].BaseValue = 2m;
        }
        if (count >= 3)
        {
            DynamicVars["Cleanse"].BaseValue = 1m;
        }
        if (count >= 4)
        {
            DynamicVars["IntangiblePower"].BaseValue = 1m;
        }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        Creature self = Owner.Creature;

        if (DynamicVars["StrengthPower"].BaseValue > 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, self, DynamicVars["StrengthPower"].BaseValue, self, null);
        }
        if (DynamicVars["Heal"].BaseValue > 0)
        {
            await CreatureCmd.Heal(self, DynamicVars["Heal"].BaseValue);
        }
        if (DynamicVars["Energy"].IntValue > 0)
        {
            await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
        }
        if (DynamicVars["Cleanse"].IntValue > 0 &&
            self.Powers.FirstOrDefault(p => p.Type == PowerType.Debuff) is { } debuff)
        {
            await PowerCmd.Remove(debuff);
        }
        if (DynamicVars["IntangiblePower"].BaseValue > 0)
        {
            await PowerCmd.Apply<IntangiblePower>(choiceContext, self, DynamicVars["IntangiblePower"].BaseValue, self, null);
        }
    }
}
