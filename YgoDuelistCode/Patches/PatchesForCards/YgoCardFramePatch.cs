using Godot;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Patches;

internal static class YgoSetCardVisualHelper
{
    public static readonly Color SetOrFaceDownTint = new(0.7f, 0.43f, 0.37f, 1f);
    private static readonly HashSet<int> LoggedMonsterIds = new();

    public static bool ShouldUseSetFrame(CardModel model)
    {
        if (model is AbstractMonsterCard monster)
        {
            LogMonsterSetVisualStateOnce(monster);
            if (monster.FaceDown)
                return true;
            if (monster.Pile?.Type == PileType.Hand
                && monster.WillSet
                && !monster.IsAttackBattlePosition
                && !monster.IsHandEffectFormActive
                && monster.YgoCardType != YgoCardType.FusionMonster)
                return true;
        }

        if (model is BaseSpellCard spell)
        {
            if (spell.FaceDown || spell.WasSetIntoSpellTrapZone)
                return true;
            if (spell.Pile?.Type == PileType.Hand && spell.IsSetModeInHand)
                return true;
        }

        if (model is BaseTrapCard trap)
        {
            if (trap.ShouldUseFaceDownPresentation() || IsCardCurrentlyInOwnersHand(trap))
                return true;
        }

        return false;
    }

    private static bool IsCardCurrentlyInOwnersHand(CardModel card)
    {
        var owner = card.Owner;
        if (owner == null)
            return false;

        var hand = PileType.Hand.GetPile(owner);
        return hand?.Cards?.Any(c => ReferenceEquals(c, card)) == true;
    }

    private static void LogMonsterSetVisualStateOnce(AbstractMonsterCard monster)
    {
        if (monster.Pile?.Type != PileType.Hand)
            return;

        int key = monster.GetHashCode();
        if (!LoggedMonsterIds.Add(key))
            return;

        bool shouldUseSetVisual = monster.FaceDown
                                  || (monster.WillSet
                                      && !monster.IsAttackBattlePosition
                                      && !monster.IsHandEffectFormActive
                                      && monster.YgoCardType != YgoCardType.FusionMonster);
        GD.Print(
            $"[YgoSetVisualDebug] Id={monster.Id.Entry}, YgoCardType={monster.YgoCardType}, IsHandEffectFormActive={monster.IsHandEffectFormActive}, WillSet={monster.WillSet}, FaceDown={monster.FaceDown}, ShouldUseSetVisual={shouldUseSetVisual}");
    }
}

[HarmonyPatch(typeof(CardModel), "get_Frame")]
public static class YgoCardFramePatch
{
    private const string FrameFolder = "card_frames";

    // Run after any other get_Frame modifications so our custom textures always win.
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not IYgoCard ygo)
            return;

        string? fileName = ygo.YgoCardType switch
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

[HarmonyPatch(typeof(CardModel), "get_PortraitBorder")]
public static class YgoSetModePortraitBorderPatch
{
    private const string SetFrameFile = "Inverted_Lip_Set.png";
    private const string FrameFolder = "card_frames";

    // IMPORTANT: do NOT apply this set-frame swap to CardModel.get_Frame.
    // get_Frame is the full frame layer and replacing it changes the whole card background.
    // The set lip visual belongs on PortraitBorder (the layer around portrait/type plaque).
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.Type != CardType.Skill || !YgoSetCardVisualHelper.ShouldUseSetFrame(__instance))
            return;

        string setPath = $"{FrameFolder}/{SetFrameFile}".ImagePath();
        if (!ResourceLoader.Exists(setPath))
            return;

        Texture2D? setFrame = ResourceLoader.Load<Texture2D>(setPath, null, ResourceLoader.CacheMode.Reuse);
        if (setFrame != null)
            __result = setFrame;
    }

}

/// <summary>
/// Vanilla <c>card.tscn</c> lists <c>%Frame</c> after <c>%PortraitCanvasGroup</c>, so the frame draws on top of the
/// portrait (fine when the frame texture has a transparent portrait window). YGO frames are full-card art and must
/// paint <em>behind</em> the portrait and other chrome — use sibling order, not negative <c>z_index</c> (which breaks
/// stacking vs other UI / canvas layers).
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoCardFrameDrawOrderPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        if (__instance.Model is not IYgoCard)
            return;

        Control body = __instance.Body;
        var frame = body.GetNodeOrNull<TextureRect>("%Frame");
        var portraitGroup = body.GetNodeOrNull<CanvasGroup>("%PortraitCanvasGroup");
        if (frame == null || portraitGroup == null)
            return;

        if (frame.GetParent() != body || portraitGroup.GetParent() != body)
            return;

        // Lower index = drawn first = behind. Frame must be before the portrait group.
        if (frame.GetIndex() < portraitGroup.GetIndex())
            return;

        body.MoveChild(frame, portraitGroup.GetIndex());
    }
}
