using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Oxidizers buff (counter): the next potion the player plays this turn is played again — its effect
/// re-executes right after the first resolution. One stack consumed per potion; expires at end of turn
/// (the base-game Burst pattern). The replay itself lives in
/// <see cref="Patches.OxidizersReplayPatch" /> — a postfix on <c>PotionModel.OnUseWrapper</c>, because
/// only the wrapper has the live <c>PlayerChoiceContext</c>: potions like Colorless Potion open a
/// choose-a-card screen from <c>OnUse</c>, so the replay must use the real context (a
/// <c>ThrowingPlayerChoiceContext</c> throws the moment such a potion prompts).
/// </summary>
public sealed class OxidizersPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Protected <c>Flash</c> exposed for the replay patch.</summary>
    internal void FlashForReplay() => Flash();

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
