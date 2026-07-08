using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Powers;

public sealed class BramblesPower : WickenPower
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	/// <summary>Bramble-gain signature: green spore burst on the gainer, every stack gain.</summary>
	public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		WickenFx.SporePuff(Owner);
		await Task.CompletedTask;
	}

	public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == Owner && dealer != null && (props.IsPoweredAttack() || cardSource is Omnislice))
		{
			Flash();
			// Thorn retaliation visual: swamp-green slice on the attacker (preloaded via Wicken.ExtraAssetPaths).
			WickenFx.BrambleSlice(dealer);
			await CreatureCmd.Damage(choiceContext, dealer, Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null);
			// Hemlock: bramble retaliation also seeds the attacker with 1 Hex.
			if (Owner.GetPowerAmount<HemlockPower>() > 0)
			{
				await PowerCmd.Apply<HexPower>(choiceContext, dealer, 1m, Owner, null);
			}
			// Hedge Prison makes brambles permanent: skip the per-trigger decrement.
			if (Owner.GetPowerAmount<HedgePrisonPower>() == 0)
			{
				await PowerCmd.Decrement(this);
			}
		}
	}
}
