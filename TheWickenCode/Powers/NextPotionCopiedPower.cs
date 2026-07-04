using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Gather Herbs buff (counter): the next potion the player creates is duplicated — when it's procured, a fresh
/// copy is procured straight back into the belt. One stack is consumed per creation. The instance-scoped
/// <c>_copying</c> guard makes the copy's own procurement a no-op, so a single creation only ever yields one copy
/// (and stacked Gather Herbs spreads across multiple distinct creations rather than cascading on one). Belt-full
/// copies just fail silently. Combat-scoped: the buff is cleared at combat end, so only in-combat creation
/// triggers it.
/// </summary>
public sealed class NextPotionCopiedPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _copying;

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        // Never copy The Cauldron (its state lives in the instance; a copy would be a fresh empty one) —
        // and don't consume a stack for it: the buff waits for a copyable potion.
        if (_copying || potion.Owner != Owner.Player || potion is Potions.TheCauldron)
        {
            return;
        }
        PotionModel? canonical = ModelDb.AllPotions.FirstOrDefault(p => p.GetType() == potion.GetType());
        if (canonical == null)
        {
            return;
        }
        await PowerCmd.Decrement(this);
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
