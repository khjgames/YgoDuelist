using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Basic;

internal static class VanillaBorrowedPortraitPaths
{
    public static string PackedPng<T>() where T : CardModel
    {
        var c = ModelDb.Card<T>();
        return ImageHelper.GetImagePath($"packed/card_portraits/{c.Pool.Title.ToLowerInvariant()}/{c.Id.Entry.ToLowerInvariant()}.png");
    }
}
