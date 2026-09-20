using BaseLib.Abstracts;
using Godot;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Character;

public class AugurCardPool : CustomCardPoolModel
{
    public override string Title => Augur.CharacterId; //This is not a display name.

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // HSV shader over the base card back; see WitchCardPool for the knobs.
    public override float H => 0.58f;
    public override float S => 0.35f;
    public override float V => 0.95f;

    //Color of small card icons
    public override Color DeckEntryCardColor => new("7FA3C9");

    public override bool IsColorless => false;
}
