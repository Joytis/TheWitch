using BaseLib.Abstracts;
using Godot;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Home of the Witch's payload-only potions (rarity <c>Token</c>): the ones a card/relic/effect grants via
/// <c>PotionCmd.TryToProcure</c> and that must never come out of a "random potion" roll.
///
/// Why a separate pool and not just Token rarity: <c>PotionFactory</c> filters by rarity, but four base-game
/// events (The Legends Were True, Battleworn Dummy, Endless Conveyor, Wellspring) pick uniformly from
/// <c>Character.PotionPool + SharedPotionPool</c> with no rarity filter at all — the base game only stays
/// clean because its own token/event potions live in <c>TokenPotionPool</c>/<c>EventPotionPool</c>, which
/// those events never enumerate. This pool mirrors that: <see cref="IsShared" /> puts it in
/// <c>ModelDb.AllSharedPotionPools</c> (so <c>PotionModel.Pool</c> resolves and the Potion Lab lists them)
/// without ever being a character's <c>PotionPool</c>.
///
/// Potion Lab consequence (same as base-game Potion-Shaped Rock): the Special tab sorts these with the
/// shared/alphabetical group and draws no character-coloured outline, because both lookups only walk
/// <c>AllCharacterPotionPools</c>.
/// </summary>
public class WitchTokenPotionPool : CustomPotionPoolModel
{
    public override bool IsShared => true;

    public override Color LabOutlineColor => Witch.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
