using BaseLib.Abstracts;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Character;

namespace YgoDuelist.YgoDuelistCode.Potions;

[Pool(typeof(YgoDuelistPotionPool))]
public abstract class YgoDuelistPotion : CustomPotionModel;