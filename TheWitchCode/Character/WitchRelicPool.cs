using BaseLib.Abstracts;
using TheWitch.TheWitchCode.Extensions;
using Godot;

namespace TheWitch.TheWitchCode.Character;

public class WitchRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Witch.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}