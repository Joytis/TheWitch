using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Powers;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class BramblesPower : WickenPower
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == Owner && dealer != null && (props.IsPoweredAttack() || cardSource is Omnislice))
		{
			Flash();
			// Vicious Barbs adds flat damage to each bramble retaliation.
			int bonus = Owner.GetPowerAmount<ViciousBarbsPower>();
			await CreatureCmd.Damage(choiceContext, dealer, Amount + bonus, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null);
			// Hedge Prison makes brambles permanent: skip the per-trigger decrement.
			if (Owner.GetPowerAmount<HedgePrisonPower>() == 0)
			{
				await PowerCmd.Decrement(this);
			}
		}
	}
}
