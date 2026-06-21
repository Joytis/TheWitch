using BaseLib.Abstracts;
using BaseLib.Utils;
using TheWicken.TheWickenCode.Character;

namespace TheWicken.TheWickenCode.Potions;

[Pool(typeof(TheWickenPotionPool))]
public abstract class TheWickenPotion : CustomPotionModel;