using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCards;

/// <summary>
/// Keyword tips from <see cref="HoverTipFactory.FromKeyword"/> use a plain <see cref="HoverTip"/> with
/// <c>IsDebuff == false</c>. The Doomed keyword (<c>card_keywords.json</c> id 20048) should read like a downside
/// next to <see cref="DoomPower"/> — same red debuff styling as other powers with <see cref="PowerType.Debuff"/>.
/// </summary>
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
public static class DoomedKeywordRaDoomedPowerHoverTipPatch
{
    /// <summary>Matches <c>20048.title</c> / <c>DoomedKeyword</c> in YGO cards.</summary>
    private const CardKeyword DoomedKeywordId = (CardKeyword)20048;

    [HarmonyPrefix]
    public static bool Prefix(CardKeyword keyword, ref IHoverTip __result)
    {
        if (keyword != DoomedKeywordId)
            return true;

        __result = HoverTipFactory.FromPower<RaDoomedPower>();
        return false;
    }
}
