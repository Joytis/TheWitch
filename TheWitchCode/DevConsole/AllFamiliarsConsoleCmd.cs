using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.DevConsole;

/// <summary>
/// Dev console: <c>allfamiliars [stacks]</c> — applies every familiar power to the issuing player
/// (default 1 stack each). Spawns the full pet menagerie for testing positions/animations.
/// </summary>
public sealed class AllFamiliarsConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "allfamiliars";

    public override string Args => "[stacks]";

    public override string Description => "Applies every familiar power to you (optional stack count, default 1).";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Creature == null || !CombatManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "Must be in combat.");
        }

        decimal stacks = 1m;
        if (args.Length > 0)
        {
            if (!int.TryParse(args[0], out int parsed) || parsed < 1)
            {
                return new CmdResult(success: false, "Arg 1 must be a positive stack count.");
            }

            stacks = parsed;
        }

        Task task = ApplyAll(issuingPlayer, stacks);
        return new CmdResult(task, success: true, $"Applied all familiar powers x{stacks}.");
    }

    private static async Task ApplyAll(Player player, decimal stacks)
    {
        var creature = player.Creature;
        await PowerCmd.Apply<BearFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
        await PowerCmd.Apply<CatFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
        await PowerCmd.Apply<CrowFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
        await PowerCmd.Apply<OwlFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
        await PowerCmd.Apply<RatFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
        await PowerCmd.Apply<WolfFamiliarPower>(new BlockingPlayerChoiceContext(), creature, stacks, creature, null);
    }
}
