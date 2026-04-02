namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Which rolled pack group a shop grid cell belongs to (see <see cref="YgoMerchantOfferGenerator"/>).</summary>
public enum YgoMerchantShopPackRole
{
    DoubleFirst,
    DoubleSecond,
    /// <summary>First single-tag pack (4 cells on the 3×6 grid).</summary>
    SingleTagFirst,
    /// <summary>Second single-tag pack (4 cells on the 3×6 grid).</summary>
    SingleTagSecond,
    Filler
}
