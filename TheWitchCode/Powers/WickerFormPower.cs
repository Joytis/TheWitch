using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Wicker Form (passive marker): while active, ANY card or potion the owner would create is replaced by a
/// Rake. The replacement itself lives in <see cref="Patches.WickerFormReplacementPatch" /> — cards are
/// swapped at the <c>CardPileCmd.AddGeneratedCardsToCombat</c> funnel, potions at
/// <c>PotionCmd.TryToProcure</c>. This class is just the registered toggle.
/// </summary>
public sealed class WickerFormPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;
}
