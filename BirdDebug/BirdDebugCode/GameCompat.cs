using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
#if STS2_BETA
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves;
#endif

namespace Bird.BirdDebug;

/// <summary>
/// The game calls whose signature differs between the public and the beta game branch, so each
/// difference is spelled out once instead of at every call site. STS2_BETA is defined by a build
/// with -p:Sts2Branch=beta (Sts2PathDiscovery.props).
/// </summary>
internal static class GameCompat
{
    /// <summary>A fresh unsaved Standard singleplayer run (default acts, no modifiers).</summary>
    public static async Task StartSingleplayerRun(NGame game, CharacterModel character, string seed)
    {
#if STS2_BETA
        // 0.113 takes the Player (not the character) and an explicit isCustomGame.
        Player player = Player.CreateForNewRun(character, SaveManager.Instance.GenerateUnlockStateFromProgress(), 1uL);
        await game.StartNewSingleplayerRun(player, shouldSave: false, ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(), seed, GameMode.Standard, isCustomGame: false);
#else
        await game.StartNewSingleplayerRun(character, shouldSave: false, ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(), seed, GameMode.Standard);
#endif
    }

    /// <summary>A combat for a throwaway test run.</summary>
    public static CombatState NewCombatState(EncounterModel encounter, RunState run)
    {
#if STS2_BETA
        return new CombatState(encounter, run, run.Modifiers, run.BadgeModels, run.RunModels);   // 0.113: MultiplayerScalingModel -> RunModels
#else
        return new CombatState(encounter, run, run.Modifiers, run.BadgeModels, run.MultiplayerScalingModel);
#endif
    }
}
