using BaseLib.Abstracts;
using Godot;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Character;

public class AugurPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Augur.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
