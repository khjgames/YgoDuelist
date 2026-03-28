using MegaCrit.Sts2.Core.Helpers;

namespace YgoDuelist.YgoDuelistCode.Cards.Basic;

internal static class SilentStarterPortraitPaths
{
    public const string StrikeEntry = "strike_silent";
    public const string DefendEntry = "defend_silent";

    public static string PackedPng(string entryLower) =>
        ImageHelper.GetImagePath($"packed/card_portraits/silent/{entryLower}.png");

    public static string AtlasTres(string entryLower) =>
        ImageHelper.GetImagePath($"atlases/card_atlas.sprites/silent/{entryLower}.tres");

    public static string BetaAtlasTres(string entryLower) =>
        ImageHelper.GetImagePath($"atlases/card_atlas.sprites/silent/beta/{entryLower}.tres");
}
