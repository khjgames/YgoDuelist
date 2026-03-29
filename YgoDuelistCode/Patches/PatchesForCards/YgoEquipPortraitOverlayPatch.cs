using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Equip-link portrait overlay on zone equip spells when their attached duel monster is hovered/focused.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
[HarmonyPriority(Priority.Last)]
[HarmonyAfter("YgoDuelist.YgoDuelistCode.Patches.YgoSetModePlaqueAndFrameTintPatch")]
public static class YgoEquipPortraitOverlayPatch
{
    private const string EquipPortraitPath = "YgoDuelist/images/card_portraits/equip_portrait_overlay.png";
    private const string EquipPortraitOverlayNodeName = "YgoEquipLinkPortraitOverlay";

    private static Texture2D? _equipPortraitOverlayTexture;

    [HarmonyPostfix]
    public static void ReloadPostfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        var model = __instance.Model;
        var body = __instance.Body;
        if (model == null || body == null)
            return;

        TextureRect? portrait = body.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait == null)
            return;

        TextureRect? overlay = EnsurePortraitOverlayNode(body, portrait);
        if (overlay == null)
            return;
        SyncOverlayToPortrait(overlay, portrait);

        _equipPortraitOverlayTexture ??= ResourceLoader.Load<Texture2D>(EquipPortraitPath, null, ResourceLoader.CacheMode.Reuse);
        overlay.Texture = _equipPortraitOverlayTexture;

        bool showEquipLink = false;
        if (YgoSpellTrapZoneBridge.IsInZone(model) && model.Owner != null)
        {
            if (model is BaseEquipSpellCard eq)
                showEquipLink = ShouldShowEquipLinkOverlayForEquip(model.Owner, eq);
            else if (model is IYgoSpellTrapEquipLink)
                showEquipLink = ShouldShowEquipLinkOverlayForLinkTrap(model.Owner, model);
        }

        overlay.Visible = showEquipLink && portrait.Visible && _equipPortraitOverlayTexture != null;
    }

    private static bool ShouldShowEquipLinkOverlayForEquip(Player player, BaseEquipSpellCard equip)
    {
        var pet = DuelMonsterHoverTrackerPatch.CurrentHoveredPet;
        if (pet == null || pet.PetOwner != player)
            return false;

        var sourceMonster = DuelMonsterFieldRegistry.GetSourceCardForPet(pet) as BaseMonsterCard;
        if (sourceMonster == null)
            return false;

        return ReferenceEquals(YgoEquipSpellRegistry.GetEquippedMonster(equip), sourceMonster);
    }

    private static bool ShouldShowEquipLinkOverlayForLinkTrap(Player player, CardModel trapModel)
    {
        var pet = DuelMonsterHoverTrackerPatch.CurrentHoveredPet;
        if (pet == null || pet.PetOwner != player)
            return false;

        var sourceMonster = DuelMonsterFieldRegistry.GetSourceCardForPet(pet) as BaseMonsterCard;
        if (sourceMonster == null)
            return false;

        return ReferenceEquals(YgoSpellTrapEquipLinkRegistry.GetLinkedMonster(trapModel), sourceMonster);
    }

    private static TextureRect? EnsurePortraitOverlayNode(Control body, TextureRect portrait)
    {
        Node? portraitParentNode = portrait.GetParent();
        if (portraitParentNode == null)
            return null;
        Node portraitParent = portraitParentNode;

        TextureRect? staleInBody = body.GetNodeOrNull<TextureRect>(EquipPortraitOverlayNodeName);
        if (staleInBody != null && staleInBody.GetParent() != portraitParent)
        {
            staleInBody.GetParent()?.RemoveChild(staleInBody);
            staleInBody.QueueFree();
        }

        TextureRect? existing = portraitParent.GetNodeOrNull<TextureRect>(EquipPortraitOverlayNodeName);
        if (existing != null)
        {
            portraitParent.MoveChild(existing, portrait.GetIndex() + 1);
            return existing;
        }

        var overlay = new TextureRect
        {
            Name = EquipPortraitOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
            Visible = false,
        };

        portraitParent.AddChild(overlay);
        portraitParent.MoveChild(overlay, portrait.GetIndex() + 1);
        return overlay;
    }

    private static void SyncOverlayToPortrait(TextureRect overlay, TextureRect portrait)
    {
        overlay.AnchorLeft = portrait.AnchorLeft;
        overlay.AnchorTop = portrait.AnchorTop;
        overlay.AnchorRight = portrait.AnchorRight;
        overlay.AnchorBottom = portrait.AnchorBottom;

        overlay.OffsetLeft = portrait.OffsetLeft;
        overlay.OffsetTop = portrait.OffsetTop;
        overlay.OffsetRight = portrait.OffsetRight;
        overlay.OffsetBottom = portrait.OffsetBottom;

        overlay.Position = portrait.Position;
        overlay.Size = portrait.Size;
        overlay.Scale = portrait.Scale;
        overlay.Rotation = portrait.Rotation;
        overlay.PivotOffset = portrait.PivotOffset;
        overlay.SelfModulate = Colors.White;
        overlay.Modulate = Colors.White;
    }
}
