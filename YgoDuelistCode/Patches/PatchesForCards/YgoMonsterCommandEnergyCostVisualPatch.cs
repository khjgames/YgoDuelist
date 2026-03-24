using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After <see cref="NCard"/> energy UI updates: for command cards with <see cref="MonsterCommandCard.CustomCommandEnergyTexturePath"/>,
/// face-up <see cref="BaseFieldSpellCard"/> in the Spell/Trap zone, face-up <see cref="BaseContinuousSpellCard"/> in that zone,
/// and face-up equipped <see cref="BaseEquipSpellCard"/> in that zone,
/// swaps the unplayable overlay to the invisible orb texture
/// and blanks the cost label (same as Exit_Monster_Options).
/// Restores the vanilla unplayable texture when pooled <see cref="NCard"/> instances show other cards.
/// </summary>
[HarmonyPatch]
public static class YgoMonsterCommandEnergyCostVisualPatch
{
    private static Texture2D? _cachedUnplayableTexture;
    private static string? _cachedUnplayablePath;

    private static readonly ConditionalWeakTable<NCard, Texture2D> DefaultUnplayableTextureByNCard = new();

    static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NCard), "UpdateEnergyCostVisuals", new[] { typeof(PileType) });

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        var model = __instance.Model;
        if (model == null)
            return;

        string? customPath = model is MonsterCommandCard mccPath ? mccPath.CustomCommandEnergyTexturePath : null;
        if (string.IsNullOrEmpty(customPath)
            && model is BaseFieldSpellCard zoneField
            && model.Pile?.Type == SpellTrapZonePile.CustomType
            && !zoneField.FaceDown)
            customPath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        else if (string.IsNullOrEmpty(customPath)
                 && model is BaseContinuousSpellCard zoneCont
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneCont.FaceDown)
            customPath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;
        else if (string.IsNullOrEmpty(customPath)
                 && model is BaseEquipSpellCard zoneEq
                 && model.Pile?.Type == SpellTrapZonePile.CustomType
                 && !zoneEq.FaceDown
                 && zoneEq.EquippedMonster != null)
            customPath = BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

        var useCustomUnplayable = !string.IsNullOrEmpty(customPath);

        var unplayable = __instance.GetNodeOrNull<TextureRect>("%UnplayableEnergyIcon");
        if (unplayable != null)
        {
            if (useCustomUnplayable)
            {
                if (!DefaultUnplayableTextureByNCard.TryGetValue(__instance, out _))
                {
                    var t = unplayable.Texture;
                    if (t != null)
                        DefaultUnplayableTextureByNCard.Add(__instance, t);
                }

                if (_cachedUnplayablePath != customPath || _cachedUnplayableTexture == null)
                {
                    _cachedUnplayablePath = customPath;
                    _cachedUnplayableTexture = ResourceLoader.Load<Texture2D>(customPath!, null, ResourceLoader.CacheMode.Reuse);
                }

                if (_cachedUnplayableTexture != null)
                    unplayable.Texture = _cachedUnplayableTexture;
            }
            else if (DefaultUnplayableTextureByNCard.TryGetValue(__instance, out var original))
            {
                unplayable.Texture = original;
            }
        }

        if (!useCustomUnplayable)
            return;

        if (__instance.Visibility != ModelVisibility.Visible)
            return;
        if (model.EnergyCost.CostsX)
            return;

        bool zoneHideEnergyLikeFaceUpField = model is BaseFieldSpellCard fs
                                             && model.Pile?.Type == SpellTrapZonePile.CustomType
                                             && !fs.FaceDown
                                             || model is BaseContinuousSpellCard ct
                                             && model.Pile?.Type == SpellTrapZonePile.CustomType
                                             && !ct.FaceDown
                                             || model is BaseEquipSpellCard eq
                                             && model.Pile?.Type == SpellTrapZonePile.CustomType
                                             && !eq.FaceDown
                                             && eq.EquippedMonster != null;

        if (!zoneHideEnergyLikeFaceUpField)
        {
            if (model is not MonsterCommandCard)
                return;
            if (model.EnergyCost.GetWithModifiers(CostModifiers.All) != 0)
                return;
        }

        var label = __instance.GetNodeOrNull<MegaLabel>("%EnergyLabel");
        label?.SetTextAutoSize(" ");
    }
}
