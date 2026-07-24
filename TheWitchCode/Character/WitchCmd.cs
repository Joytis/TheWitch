using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions;

public static class WitchCmd
{
    public static async Task TryProcureNoxiousBrews(Player player, int count)
    {
        WitchFx.Splash(player.Creature, WitchFx.Purple); // conjured brew: purple splash
        for (int i = 0; i < count; i++)
        {
            await PotionCmd.TryToProcure<NoxiousBrew>(player);
        }
    }

}