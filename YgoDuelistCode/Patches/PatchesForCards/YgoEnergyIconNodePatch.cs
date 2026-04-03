using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Basic;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoEnergyIconNodePatch
{
    private const string AttackMonsterEnergyPath = "YgoDuelist/images/card_frames/attack_monster_energy_icon.png";
    private const string DefenseMonsterEnergyPath = "YgoDuelist/images/card_frames/defense_monster_energy_icon.png";

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        var model = __instance?.Model;
        if (model == null)
            return;

        var icon = __instance.GetNodeOrNull<TextureRect>("%EnergyIcon");
        if (icon == null)
            return;

        if (model is Strike_YgoDuelist)
        {
            icon.Visible = true;
            var tex = ResourceLoader.Load<Texture2D>(AttackMonsterEnergyPath, null, ResourceLoader.CacheMode.Reuse);
            if (tex != null)
                icon.Texture = tex;
            return;
        }

        if (model is Defend_YgoDuelist)
        {
            icon.Visible = true;
            var defTex = ResourceLoader.Load<Texture2D>(DefenseMonsterEnergyPath, null, ResourceLoader.CacheMode.Reuse);
            if (defTex != null)
                icon.Texture = defTex;
            return;
        }

        string? energyPrefix = null;
        string? customTexturePath = null;

        if (model is MonsterCommandCard mcc)
        {
            var commandCustom = mcc.CustomCommandEnergyTexturePath;
            if (!string.IsNullOrEmpty(commandCustom))
            {
                icon.Visible = true;
                customTexturePath = commandCustom;
            }
            else if (!mcc.ShowsEnergyCostIcon)
            {
                icon.Visible = false;
                return;
            }
            else
            {
                icon.Visible = true;
                string? commandPrefix = mcc.CommandEnergyIconPrefix;
                if (!string.IsNullOrEmpty(commandPrefix))
                    energyPrefix = commandPrefix;
                else if (model.Type == CardType.Attack)
                    customTexturePath = AttackMonsterEnergyPath;
                else
                    customTexturePath = DefenseMonsterEnergyPath;
            }
        }
        else if (model is AbstractMonsterCard handEffectMonster && handEffectMonster.IsHandEffectFormActive)
        {
            icon.Visible = true;
            energyPrefix = "silent";
        }
        else if (model is BaseFieldSpellCard zoneField
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneField.FaceDown)
        {
            icon.Visible = true;
            customTexturePath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        }
        else if (model is BaseContinuousSpellCard zoneContinuous
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneContinuous.FaceDown)
        {
            icon.Visible = true;
            customTexturePath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        }
        else if (model is BaseEquipSpellCard zoneEquip
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneEquip.FaceDown
                 && zoneEquip.EquippedMonster != null)
        {
            icon.Visible = true;
            customTexturePath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        }
        else if (model is BaseContinuousTrapCard zoneContinuousTrap
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneContinuousTrap.FaceDown)
        {
            icon.Visible = true;
            customTexturePath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        }
        else if (model is BaseTrapCard zoneLinkTrap
                 && model is IYgoSpellTrapEquipLink
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneLinkTrap.FaceDown
                 && YgoSpellTrapEquipLinkRegistry.GetLinkedMonster(model) != null)
        {
            icon.Visible = true;
            customTexturePath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        }
        else if (model is IYgoCard ygo)
        {
            icon.Visible = true;
            energyPrefix = ygo.YgoCardType switch
            {
                YgoCardType.Spell => "silent",
                YgoCardType.Trap => "necrobinder",
                YgoCardType.Monster => null,
                YgoCardType.EffectMonster => null,
                YgoCardType.FusionMonster => null,
                YgoCardType.RitualMonster => null,
                _ => null
            };

            if (ygo.YgoCardType == YgoCardType.Monster
                || ygo.YgoCardType == YgoCardType.EffectMonster
                || ygo.YgoCardType == YgoCardType.FusionMonster
                || ygo.YgoCardType == YgoCardType.RitualMonster)
            {
                customTexturePath = model.Type == CardType.Attack
                    ? AttackMonsterEnergyPath
                    : DefenseMonsterEnergyPath;
            }
        }

        if (string.IsNullOrEmpty(energyPrefix) && string.IsNullOrEmpty(customTexturePath))
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

