using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// The Cauldron: card-only accumulator potion (created/filled by Witchcraft). Starts empty; Witchcraft pours the
/// rest of the belt into it — each poured potion adds Strength + healing, with bonus effects at 2/3/4+
/// poured in one cast (Energy / remove a debuff / Intangible). Token rarity so it never rolls as a drop.
/// State lives in this mutable instance's DynamicVars — potions serialize as id+slot only
/// (SerializablePotion), so poured effects survive save/quit/resume only via the sidecar file managed by
/// <see cref="CauldronSaveState" /> (not MP-synced).
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
        new PowerVar<IntangiblePower>(0m),
        new PowerVar<VigorPower>(0m),
        new DynamicVar("Block", 0m)
    ];

    /// <summary>Find the player's Cauldron, procuring one if absent (shared by every "add X to The
    /// Cauldron" card). Returns null only when the belt is full and procurement fails.</summary>
    public static async Task<TheCauldron?> EnsureInBelt(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        TheCauldron? cauldron = owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        if (cauldron == null)
        {
            await PotionCmd.TryToProcure<TheCauldron>(owner);
            cauldron = owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        }
        return cauldron;
    }

    /// <summary>Add to an arbitrary stat var by name ("VigorPower", "Block", "IntangiblePower", …) — the
    /// extension point for support cards that feed specific stats into the Cauldron.</summary>
    public void AddStat(string varName, decimal amount)
    {
        AssertMutable();
        EnsureUnsharedVars();
        DynamicVars[varName].BaseValue += amount;
        FlashInBelt();
    }

    /// <summary>Pop the belt icon (the "newly acquired" bounce) so stat gains are visible to the player.</summary>
    private void FlashInBelt() => NRun.Instance?.GlobalUi.TopBar.PotionContainer.AnimatePotion(this);

    private static readonly FieldInfo DynamicVarsField = AccessTools.Field(typeof(PotionModel), "_dynamicVars");
    private static readonly FieldInfo VarOwnerField = AccessTools.Field(typeof(DynamicVar), "_owner");

    /// <summary>
    /// <c>PotionModel.MutableClone</c> does not deep-clone <c>_dynamicVars</c> (unlike Card/Relic/Power models),
    /// so a freshly-procured Cauldron can share its var set with the CANONICAL instance — mutating it would
    /// pollute every future Cauldron. Detect a shared set (vars owned by someone else) and swap in a private
    /// clone before any write.
    /// </summary>
    private void EnsureUnsharedVars()
    {
        if (DynamicVars.Values.Any(v => !ReferenceEquals(VarOwnerField.GetValue(v), this)))
        {
            DynamicVarsField.SetValue(this, DynamicVars.Clone(this));
        }
    }

    /// <summary>
    /// Stir <paramref name="count" /> potions in: +2 Strength / +3 Heal each. The per-potion accumulation
    /// shared by every "stir a potion into the Cauldron" effect (Witchcraft). No threshold bonuses —
    /// those are Witchcraft's per-cast pour extras (see <see cref="PourPotions" />).
    /// </summary>
    public void Stir(int count = 1)
    {
        AssertMutable();
        EnsureUnsharedVars();
        DynamicVars["StrengthPower"].BaseValue += 2m * count;
        DynamicVars["Heal"].BaseValue += 3m * count;
        FlashInBelt();
    }

    /// <summary>
    /// Pour <paramref name="count" /> discarded potions in (one Witchcraft cast). Strength/healing accumulate
    /// per potion (via <see cref="Stir" />); the threshold effects (2+: Energy, 3+: cleanse, 4+: Intangible)
    /// are unlocked, not stacked.
    /// </summary>
    public void PourPotions(int count)
    {
        AssertMutable();
        EnsureUnsharedVars();
        Stir(count);
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

    /// <summary>Re-apply poured state from the sidecar save (see <c>CauldronSaveState</c>) after a run load.
    /// Keys are var names; unknown keys (from an older/newer sidecar schema) are ignored.</summary>
    public void RestoreState(IReadOnlyDictionary<string, decimal> values)
    {
        AssertMutable();
        EnsureUnsharedVars();
        foreach ((string name, decimal value) in values)
        {
            if (DynamicVars.TryGetValue(name, out DynamicVar? var))
            {
                var.BaseValue = value;
            }
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
        if (DynamicVars["VigorPower"].BaseValue > 0)
        {
            await PowerCmd.Apply<VigorPower>(choiceContext, self, DynamicVars["VigorPower"].BaseValue, self, null);
        }
        if (DynamicVars["Block"].BaseValue > 0)
        {
            await CreatureCmd.GainBlock(self, DynamicVars["Block"].BaseValue, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null);
        }
    }
}
