using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.DevConsole;

/// <summary>
/// Dev console: <c>potiontraits</c> — dumps every registered potion with its classified
/// <see cref="PotionOrientation" />, grouped by rarity. Useful for eyeballing the manual table + auto-derive
/// classification (no test suite; validation is manual).
/// </summary>
public sealed class PotionTraitsConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "potiontraits";

    public override string Args => "";

    public override string Description => "Dumps every potion's orientation, grouped by rarity.";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var byRarity = PotionCatalog.All
            .OrderBy(p => p.Rarity)
            .ThenBy(p => p.Id.Entry)
            .GroupBy(p => p.Rarity);

        StringBuilder sb = new StringBuilder().AppendLine("Potion orientations:");
        int count = 0;

        foreach (var group in byRarity)
        {
            sb.AppendLine($"== {group.Key} ==");
            foreach (var potion in group)
            {
                PotionOrientation orientation = PotionTraits.OrientationOf(potion);
                sb.AppendLine($"  {potion.Id.Entry,-24} {orientation}");
                count++;
            }
        }

        string report = sb.ToString();
        Log.Info(report);
        return new CmdResult(success: true, $"Dumped orientation for {count} potions to console & logs.\n{report}");
    }
}
