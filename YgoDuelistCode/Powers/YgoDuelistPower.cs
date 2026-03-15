using BaseLib.Abstracts;
using BaseLib.Extensions;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

public abstract class YgoDuelistPower : CustomPowerModel
{
    //Loads from YgoDuelist/images/powers/your_power.png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}