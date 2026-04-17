using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Face-down overlay, attribute/race icons, and equip-link overlay on duel monster field portraits (Sprite2D).
/// Layout constants mirror YgoMonsterLevelStripPatch relative to a nominal card portrait height.
/// </summary>
public static class DuelMonsterPortraitDecorations
{
    private const string FaceDownPath = "YgoDuelist/images/card_portraits/Face_Down_2.png";
    private const string EquipLinkPath = "YgoDuelist/images/card_portraits/equip_portrait_overlay.png";
    private const string AttributeIconFolder = "YgoDuelist/images/card_frames/Attribute";
    private const string RaceIconFolder = "YgoDuelist/images/card_frames/Race";

    private const float RefPortraitHeightPx = 380f;
    private const float IconRowHeightPx = 26f;
    private const float RaceHorizontalNudgePx = 48f;
    private const float AttributeRightEdgeLeftOfRaceRightPx = 31f;
    private const float IconRowTopFromPortraitTopPx = 52f;

    private const string NodeFaceDown = "YgoDuelMonsterFaceDownOverlay";
    private const string NodeAttr = "YgoDuelMonsterAttrIcon";
    private const string NodeRace = "YgoDuelMonsterRaceIcon";
    private const string NodeEquip = "YgoDuelMonsterEquipLinkOverlay";

    private static Texture2D? _faceDownTex;
    private static Texture2D? _equipTex;
    private static readonly Dictionary<DuelMonsterAttribute, Texture2D?> _attrTex = new();
    private static readonly Dictionary<DuelMonsterRace, Texture2D?> _raceTex = new();

    public static void RefreshAllInCombatRoom()
    {
        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        foreach (var node in room.CreatureNodes)
        {
            var pet = node.Entity;
            if (pet?.Monster is DuelMonsterModel)
                RefreshPet(pet);
        }
    }

    public static void RefreshPet(Creature? pet)
    {
        if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
            return;

        var nCreature = NCombatRoom.Instance?.GetCreatureNode(pet);
        if (nCreature == null || !GodotObject.IsInstanceValid(nCreature))
            return;

        var source = DuelMonsterFieldRegistry.GetSourceCardForPet(pet) as BaseMonsterCard;
        if (source == null)
            return;

        var sprite = GetPortraitSprite(nCreature);
        if (sprite == null || sprite.Texture == null)
            return;

        ApplyFaceDownAndIcons(sprite, source);
        ApplyEquipLinkOverlay(sprite, source);
    }

    public static void RefreshAllEquipLinkLayers()
    {
        RefreshAllInCombatRoom();
    }

    internal static Sprite2D? GetPortraitSprite(NCreature nCreature)
    {
        var body = nCreature.Visuals.GetNode<Node2D>("%Visuals");
        if (body is Sprite2D s)
            return s;
        return body.GetNodeOrNull<Sprite2D>("Portrait")
               ?? body.GetNodeOrNull<Sprite2D>("Sprite");
    }

    private static void ApplyFaceDownAndIcons(Sprite2D portrait, BaseMonsterCard source)
    {
        Vector2 texSize = portrait.Texture.GetSize();
        if (texSize.X <= 0 || texSize.Y <= 0)
            return;

        float sy = Mathf.Abs(portrait.Scale.Y);
        float sx = Mathf.Abs(portrait.Scale.X);
        float displayH = texSize.Y * sy;
        float scaleFactor = displayH / RefPortraitHeightPx;

        float halfW = texSize.X * sx * 0.5f;
        float halfH = texSize.Y * sy * 0.5f;

        bool faceDown = source.FaceDown;
        var faceNode = EnsureSpriteChild(portrait, NodeFaceDown, 4);
        _faceDownTex ??= ResourceLoader.Load<Texture2D>(FaceDownPath, null, ResourceLoader.CacheMode.Reuse);
        if (_faceDownTex != null)
        {
            faceNode.Texture = _faceDownTex;
            faceNode.Visible = faceDown;
            faceNode.Centered = true;
            faceNode.Position = Vector2.Zero;
            Vector2 fd = _faceDownTex.GetSize();
            faceNode.Scale = fd.X > 0 && fd.Y > 0
                ? new Vector2(texSize.X / fd.X, texSize.Y / fd.Y)
                : Vector2.One;
            faceNode.ZIndex = 4;
            faceNode.ZAsRelative = true;
        }
        else
        {
            faceNode.Visible = false;
        }

        if (faceDown)
        {
            HideSpriteChild(portrait, NodeAttr);
            HideSpriteChild(portrait, NodeRace);
            return;
        }

        float iconH = IconRowHeightPx * scaleFactor;
        float rowY = -halfH + IconRowTopFromPortraitTopPx * scaleFactor + iconH * 0.5f;

        var raceTex = GetRaceTexture(source.DuelMonsterRace);
        var attrTex = GetAttributeTexture(source.DuelMonsterAttribute);

        float raceW = 0f;
        if (raceTex != null)
        {
            Vector2 rsz = raceTex.GetSize();
            if (rsz.Y > 0)
                raceW = iconH * (rsz.X / rsz.Y);
        }

        float raceCenterX = halfW - RaceHorizontalNudgePx * scaleFactor - raceW * 0.5f;
        LayoutFieldIcon(portrait, NodeRace, raceTex, iconH, raceCenterX, rowY, 5);

        float attrW = 0f;
        if (attrTex != null)
        {
            Vector2 asz = attrTex.GetSize();
            if (asz.Y > 0)
                attrW = iconH * (asz.X / asz.Y);
        }

        float raceRight = raceCenterX + raceW * 0.5f;
        float attrCenterX = raceRight - AttributeRightEdgeLeftOfRaceRightPx * scaleFactor - attrW * 0.5f;
        LayoutFieldIcon(portrait, NodeAttr, attrTex, iconH, attrCenterX, rowY, 6);
    }

    private static void LayoutFieldIcon(
        Sprite2D portrait,
        string nodeName,
        Texture2D? tex,
        float iconH,
        float centerXFromPortraitCenter,
        float centerYFromPortraitCenter,
        int zIndex)
    {
        var node = EnsureSpriteChild(portrait, nodeName, zIndex);
        if (tex == null)
        {
            node.Visible = false;
            return;
        }

        Vector2 sz = tex.GetSize();
        if (sz.X <= 0 || sz.Y <= 0)
        {
            node.Visible = false;
            return;
        }

        float w = iconH * (sz.X / sz.Y);
        node.Texture = tex;
        node.Centered = true;
        node.Position = new Vector2(centerXFromPortraitCenter, centerYFromPortraitCenter);
        node.Scale = new Vector2(w / sz.X, iconH / sz.Y);
        node.ZIndex = zIndex;
        node.ZAsRelative = true;
        node.Visible = true;
    }

    private static bool ZoneEquipOrLinkTargetsMonster(CardModel zoneCard, BaseMonsterCard source)
    {
        if (zoneCard is BaseEquipSpellCard eq)
            return ReferenceEquals(YgoEquipSpellRegistry.GetEquippedMonster(eq), source);
        if (zoneCard is IYgoSpellTrapEquipLink)
            return ReferenceEquals(YgoSpellTrapEquipLinkRegistry.GetLinkedMonster(zoneCard), source);
        return false;
    }

    private static void ApplyEquipLinkOverlay(Sprite2D portrait, BaseMonsterCard source)
    {
        var node = EnsureSpriteChild(portrait, NodeEquip, 8);
        _equipTex ??= ResourceLoader.Load<Texture2D>(EquipLinkPath, null, ResourceLoader.CacheMode.Reuse);

        var hoveredZone = YgoZoneEquipHandHoverState.HoveredOrSelectedZoneEquipLink;
        bool showMonsterSide = hoveredZone != null
                               && YgoSpellTrapZoneBridge.IsInZone(hoveredZone)
                               && ZoneEquipOrLinkTargetsMonster(hoveredZone, source);

        if (_equipTex == null || !showMonsterSide)
        {
            node.Visible = false;
            return;
        }

        node.Texture = _equipTex;
        node.Centered = true;
        node.Position = Vector2.Zero;
        Vector2 texSize = portrait.Texture!.GetSize();
        Vector2 es = _equipTex.GetSize();
        if (es.X <= 0 || es.Y <= 0)
        {
            node.Visible = false;
            return;
        }

        float cover = 0.92f;
        node.Scale = new Vector2(texSize.X * cover / es.X, texSize.Y * cover / es.Y);
        node.ZIndex = 8;
        node.ZAsRelative = true;
        node.Visible = true;
    }

    private static Sprite2D EnsureSpriteChild(Sprite2D parent, string name, int zIndex)
    {
        var existing = parent.GetNodeOrNull<Sprite2D>(name);
        if (existing != null)
        {
            existing.ZIndex = zIndex;
            return existing;
        }

        var s = new Sprite2D
        {
            Name = name,
            Visible = false,
            ZAsRelative = true,
            ZIndex = zIndex,
        };
        parent.AddChild(s);
        return s;
    }

    private static void HideSpriteChild(Sprite2D parent, string name)
    {
        parent.GetNodeOrNull<Sprite2D>(name)?.Hide();
    }

    private static Texture2D? GetAttributeTexture(DuelMonsterAttribute attribute)
    {
        if (_attrTex.TryGetValue(attribute, out var c))
            return c;
        string path = $"{AttributeIconFolder}/{attribute.ToString().ToLowerInvariant()}.png";
        var loaded = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        _attrTex[attribute] = loaded;
        return loaded;
    }

    private static Texture2D? GetRaceTexture(DuelMonsterRace race)
    {
        if (_raceTex.TryGetValue(race, out var c))
            return c;
        string file = race switch
        {
            DuelMonsterRace.BeastWarrior => "Beast-Warrior.png",
            DuelMonsterRace.DivineBeast => "Divine-Beast.png",
            DuelMonsterRace.SeaSerpent => "Sea Serpent.png",
            DuelMonsterRace.WingedBeast => "Winged Beast.png",
            DuelMonsterRace.SpellNormal => "Spellcaster.png",
            DuelMonsterRace.SpellContinuous => "Continuous.png",
            DuelMonsterRace.SpellQuickPlay => "Quick-Play.png",
            DuelMonsterRace.SpellEquip => "Equip.png",
            DuelMonsterRace.SpellField => "Field.png",
            DuelMonsterRace.SpellRitual => "Ritual.png",
            DuelMonsterRace.TrapNormal => "Spellcaster.png",
            DuelMonsterRace.TrapContinuous => "Continuous.png",
            DuelMonsterRace.TrapCounter => "Counter.png",
            _ => $"{race}.png",
        };
        string path = $"{RaceIconFolder}/{file}";
        var loaded = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        _raceTex[race] = loaded;
        return loaded;
    }
}
