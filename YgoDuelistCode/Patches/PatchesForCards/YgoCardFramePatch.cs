using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_Frame")]
public static class YgoCardFramePatch
{
    private const string FrameFolder = "card_frames";

    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not IYgoCard ygo)
            return;

        string fileName = ygo.YgoCardType switch
        {
            YgoCardType.Spell => "ygo_spell.png",
            YgoCardType.Trap => "ygo_trap.png",
            YgoCardType.Monster => "ygo_monster.png",
            YgoCardType.EffectMonster => "ygo_effect_monster.png",
            YgoCardType.FusionMonster => "ygo_fusion_monster.png",
            YgoCardType.RitualMonster => "ygo_ritual_monster.png",
            _ => null
        };

        if (string.IsNullOrEmpty(fileName))
            return;

        // Use mod's images folder (YgoDuelist/images/card_frames/), not game's ImageHelper path (res://images/).
        string path = $"{FrameFolder}/{fileName}".ImagePath();
        if (!ResourceLoader.Exists(path))
            return;

        __result = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
    }
}
