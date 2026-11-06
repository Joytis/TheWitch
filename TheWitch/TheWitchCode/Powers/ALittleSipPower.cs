using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// A Little Sip: whenever the player uses a potion, gain <see cref="BramblesPerStack" /> Brambles and draw one
/// card per stack (one stack per copy played). <c>TotalBrambles</c> mirrors <c>Amount * BramblesPerStack</c> for
/// the tooltip (Incarnation pattern). <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so the effects use
/// <c>ThrowingPlayerChoiceContext</c> (the base-game ReptileTrinket pattern).
/// </summary>
public sealed class ALittleSipPower : WitchPower
{
    public const decimal BramblesPerStack = 1m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("TotalBrambles", BramblesPerStack),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    private decimal TotalBrambles => Amount * BramblesPerStack;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars["TotalBrambles"].BaseValue = TotalBrambles;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await PowerCmd.Apply<BramblesPower>(new ThrowingPlayerChoiceContext(), Owner, TotalBrambles, Owner, null);
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, potion.Owner);
        }
    }
}
