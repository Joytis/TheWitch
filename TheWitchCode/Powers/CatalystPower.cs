using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Catalyst (Ancient Power): EVERY potion the player creates is duplicated — the permanent sibling of
/// <see cref="NextPotionCopiedPower" /> (same copy logic, no stack consumed). The Cauldron is never copied
/// (its state lives in the instance; a copy would be a fresh empty one). The instance-scoped
/// <c>_copying</c> guard stops the copy's own procurement from cascading.
/// </summary>
public sealed class CatalystPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    private bool _copying;

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (_copying || potion.Owner != Owner.Player || potion is Potions.TheCauldron)
        {
            return;
        }
        PotionModel? canonical = ModelDb.AllPotions.FirstOrDefault(p => p.GetType() == potion.GetType());
        if (canonical == null)
        {
            return;
        }
        Flash();
        _copying = true;
        try
        {
            await PotionCmd.TryToProcure(canonical.ToMutable(), Owner.Player);
        }
        finally
        {
            _copying = false;
        }
    }
}
