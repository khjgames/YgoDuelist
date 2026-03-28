using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Replaces the vanilla star graphic on the star-cost row with the mod conduit art for YGO monster cards.
/// Patches both <see cref="NCard.Reload"/> and <see cref="NCard.UpdateStarCostVisuals"/> so the game
/// cannot overwrite the conduit texture when it refreshes star visuals after reload.
/// </summary>
[HarmonyPatch]
public static class YgoMonsterStarIconPatch
{
    private const string ConduitTexturePath = "YgoDuelist/images/card_frames/conduit_icon.png";

    private static Texture2D? _conduitTexture;

    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NCard), "Reload");
        yield return AccessTools.Method(typeof(NCard), "UpdateStarCostVisuals", new[] { typeof(PileType) });
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance?.Model is not AbstractMonsterCard monster)
            return;

        if (monster.IsHandEffectFormActive)
        {
            var starInHandEffect = __instance.GetNodeOrNull<TextureRect>("%StarIcon");
            if (starInHandEffect != null)
                starInHandEffect.Visible = false;

            var unplayableStarInHandEffect = __instance.GetNodeOrNull<TextureRect>("%UnplayableStarIcon");
            if (unplayableStarInHandEffect != null)
                unplayableStarInHandEffect.Visible = false;
            return;
        }

        _conduitTexture ??= ResourceLoader.Load<Texture2D>(ConduitTexturePath, null, ResourceLoader.CacheMode.Reuse);
        if (_conduitTexture == null)
            return;

        var starIcon = __instance.GetNodeOrNull<TextureRect>("%StarIcon");
        if (starIcon != null)
            starIcon.Texture = _conduitTexture;

        var unplayableStar = __instance.GetNodeOrNull<TextureRect>("%UnplayableStarIcon");
        if (unplayableStar != null)
            unplayableStar.Texture = _conduitTexture;
    }
}
