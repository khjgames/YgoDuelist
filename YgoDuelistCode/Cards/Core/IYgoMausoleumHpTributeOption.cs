namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Mausoleum of the Emperor synthetic HP row in tribute selection grids.</summary>
public interface IYgoMausoleumHpTributeOption
{
    int TributeHpLoss { get; }

    int MausoleumGridSlot { get; }
}
