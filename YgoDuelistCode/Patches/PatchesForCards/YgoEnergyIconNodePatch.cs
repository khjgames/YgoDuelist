using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoEnergyIconNodePatch
{
    private const string AttackMonsterEnergyPath = "YgoDuelist/images/card_frames/attack_monster_energy.png";

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        var model = __instance?.Model;
        if (model == null)
            return;

        string? energyPrefix = null;
        string? customTexturePath = null;

        if (model is MonsterCommandCard)
        {
            if (model.Type == CardType.Attack)
                customTexturePath = AttackMonsterEnergyPath;
            else
                energyPrefix = "defect";
        }
        else if (model is IYgoCard ygo)
        {
            energyPrefix = ygo.YgoCardType switch
            {
                YgoCardType.Spell => "silent",
                YgoCardType.Trap => "necrobinder",
                YgoCardType.Monster => model.Type == CardType.Attack ? null : "defect",
                YgoCardType.EffectMonster => model.Type == CardType.Attack ? null : "defect",
                YgoCardType.FusionMonster => model.Type == CardType.Attack ? null : "defect",
                YgoCardType.RitualMonster => model.Type == CardType.Attack ? null : "defect",
                _ => null
            };

            if ((ygo.YgoCardType == YgoCardType.Monster
                || ygo.YgoCardType == YgoCardType.EffectMonster
                || ygo.YgoCardType == YgoCardType.FusionMonster
                || ygo.YgoCardType == YgoCardType.RitualMonster)
                && model.Type == CardType.Attack)
            {
                customTexturePath = AttackMonsterEnergyPath;
            }
        }

        if (string.IsNullOrEmpty(energyPrefix) && string.IsNullOrEmpty(customTexturePath))
            return;

        var icon = __instance.GetNodeOrNull<TextureRect>("%EnergyIcon");
        if (icon == null)
            return;

        string path = !string.IsNullOrEmpty(customTexturePath)
            ? customTexturePath
            : EnergyIconHelper.GetPath(energyPrefix!);
        var texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        if (texture == null)
            return;

        icon.Texture = texture;
    }
}

