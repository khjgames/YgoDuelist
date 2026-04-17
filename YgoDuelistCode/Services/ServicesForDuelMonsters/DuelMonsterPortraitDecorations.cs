using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Face-down overlay, level star strip, attribute/race icons, and equip-link overlay on duel monster field portraits (Sprite2D).
/// Layout constants mirror YgoMonsterLevelStripPatch relative to a nominal card portrait height.
/// </summary>
public static class DuelMonsterPortraitDecorations
{
    private const string FaceDownPath = "YgoDuelist/images/card_portraits/Face_Down_4.png";
    private const string StarsStripPath = "YgoDuelist/images/card_frames/12_stars.png";
    private const string StarsStripFaceDownPath = "YgoDuelist/images/card_frames/12_stars_facedown.png";
    private const string EquipLinkPath = "YgoDuelist/images/card_portraits/equip_portrait_overlay.png";
    private const string AttributeIconFolder = "YgoDuelist/images/card_frames/Attribute";
    private const string RaceIconFolder = "YgoDuelist/images/card_frames/Race";

    private const float RefPortraitHeightPx = 380f;
    private const float AttributeSetAlpha = 0.75f;
    private const float RaceSetAlpha = 0.9f;
    private const float IconRowHeightPx = 26f;
    /// <summary>Matches YgoMonsterLevelStripPatch strip band height in reference portrait space.</summary>
    private const float StripHeightPx = 20f;
    /// <summary>Right edge inset from portrait right (positive = toward center), same sense as the card strip.</summary>
    private const float StripHorizontalNudgePx = 12f;
    /// <summary>Gap between level strip bottom and icon row top, plus extra nudge — same as card patch.</summary>
    private const float AttributeGapBelowLevelStripPx = 2f;
    private const float IconRowExtraVerticalNudgePx = 2f;
    private const float RaceHorizontalNudgePx = 48f;
    private const float AttributeRightEdgeLeftOfRaceRightPx = 31f;
    private const float IconRowTopFromPortraitTopPx = 60f;

    private const string NodeFaceDown = "YgoDuelMonsterFaceDownOverlay";
    private const string NodeLevelStrip = "YgoDuelMonsterLevelStrip";
    private const string NodeAttr = "YgoDuelMonsterAttrIcon";
    private const string NodeRace = "YgoDuelMonsterRaceIcon";
    private const string NodeEquip = "YgoDuelMonsterEquipLinkOverlay";

    private static Texture2D? _faceDownTex;
    private static Texture2D? _stripTextureFaceUp;
    private static Texture2D? _stripTextureFaceDown;
    private static AtlasTexture[]? _atlasesByLevelFaceUp;
    private static AtlasTexture[]? _atlasesByLevelFaceDown;
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
        // Only reorder the local player's pet UI: moving %HealthBar on remote creatures can desync MP UI/navigation.
        if (pet.PetOwner != null && LocalContext.IsMe(pet.PetOwner))
            TryMoveDuelPetHealthBarLast(nCreature);
    }

    /// <summary>Keep %HealthBar after Visuals so it draws on top of the portrait stack (pets only).</summary>
    private static void TryMoveDuelPetHealthBarLast(NCreature nCreature)
    {
        if (!GodotObject.IsInstanceValid(nCreature) || nCreature.Entity.IsPlayer)
            return;

        Control? hb = nCreature.GetNodeOrNull<Control>("%HealthBar");
        if (hb == null || hb.GetParent() != nCreature)
            return;

        int last = nCreature.GetChildCount() - 1;
        if (hb.GetIndex() != last)
            nCreature.MoveChild(hb, last);
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
        var faceNode = EnsureSpriteChild(portrait, NodeFaceDown, 0);
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
            faceNode.ZIndex = 0;
            faceNode.ZAsRelative = true;
        }
        else
        {
            faceNode.Visible = false;
        }

        float iconH = IconRowHeightPx * scaleFactor * 2.3f;
        float rowY = -halfH + IconRowTopFromPortraitTopPx * scaleFactor + iconH * 0.5f;

        LayoutLevelStrip(portrait, source, halfW, rowY, iconH, scaleFactor);

        var raceTex = GetRaceTexture(source.DuelMonsterRace);
        var attrTex = GetAttributeTexture(source.DuelMonsterAttribute);

        float raceW = 0f;
        if (attrTex != null)
        {
            Vector2 rsz = attrTex.GetSize();
            if (rsz.Y > 0)
                raceW = iconH * (rsz.X / rsz.Y);
        }

        float raceCenterX = halfW - RaceHorizontalNudgePx * scaleFactor - (raceW * 1.35f);
        float attrIconAlpha = faceDown ? AttributeSetAlpha : 1f;
        LayoutFieldIcon(portrait, NodeRace, attrTex, iconH, raceCenterX, rowY, 2, attrIconAlpha);

        float attrW = 0f;
        if (raceTex != null)
        {
            Vector2 asz = raceTex.GetSize();
            if (asz.Y > 0)
                attrW = iconH * (asz.X / asz.Y);
        }

        float attrCenterX = raceCenterX + (raceW * 1.4f);
        float raceIconAlpha = faceDown ? RaceSetAlpha : 1f;
        LayoutFieldIcon(portrait, NodeAttr, raceTex, iconH, attrCenterX, rowY, 3, raceIconAlpha);
    }

    /// <summary>
    /// Right-aligned star strip above the attribute/race row; same atlas slices and face-down texture as
    /// <c>YgoMonsterLevelStripPatch</c>.
    /// </summary>
    private static void LayoutLevelStrip(
        Sprite2D portrait,
        BaseMonsterCard source,
        float halfW,
        float rowY,
        float iconH,
        float scaleFactor)
    {
        bool useFaceDownStrip = source.FaceDown;
        int level = Mathf.Clamp(source.GetEffectiveDuelMonsterLevel(), 1, 12);

        Texture2D? stripTexture = GetStripTexture(useFaceDownStrip);
        AtlasTexture[]? atlases = EnsureAtlases(stripTexture, useFaceDownStrip);
        var node = EnsureSpriteChild(portrait, NodeLevelStrip, 1);

        if (stripTexture == null || atlases == null)
        {
            node.Visible = false;
            return;
        }

        AtlasTexture atlas = atlases[level - 1];
        Vector2 sz = atlas.GetSize();
        if (sz.X <= 0 || sz.Y <= 0)
        {
            node.Visible = false;
            return;
        }

        float th = sz.Y;
        float regionW = sz.X;
        float stripH = StripHeightPx * scaleFactor * 2.3f;
        float targetW = stripH * (regionW / th);
        float portraitW = halfW * 2f;
        if (targetW > portraitW)
            targetW = portraitW;

        float gap = (AttributeGapBelowLevelStripPx + IconRowExtraVerticalNudgePx) * scaleFactor;
        float stripCenterY = rowY - iconH * 0.5f - gap - stripH * 0.5f;

        float stripRight = halfW - StripHorizontalNudgePx * scaleFactor;
        float stripCenterX = stripRight - targetW * 0.5f;

        node.Texture = atlas;
        node.Modulate = Colors.White;
        node.Centered = true;
        node.Position = new Vector2(stripCenterX, stripCenterY - 15f);
        node.Scale = new Vector2(targetW / regionW, stripH / th);
        node.ZIndex = 1;
        node.ZAsRelative = true;
        node.Visible = true;
    }

    private static Texture2D? GetStripTexture(bool useFaceDownStrip)
    {
        if (useFaceDownStrip)
        {
            _stripTextureFaceDown ??= ResourceLoader.Load<Texture2D>(StarsStripFaceDownPath, null, ResourceLoader.CacheMode.Reuse);
            return _stripTextureFaceDown;
        }

        _stripTextureFaceUp ??= ResourceLoader.Load<Texture2D>(StarsStripPath, null, ResourceLoader.CacheMode.Reuse);
        return _stripTextureFaceUp;
    }

    private static AtlasTexture[]? EnsureAtlases(Texture2D? strip, bool useFaceDownStrip)
    {
        if (strip == null)
            return null;

        AtlasTexture[]? current = useFaceDownStrip ? _atlasesByLevelFaceDown : _atlasesByLevelFaceUp;
        if (current != null)
            return current;

        Vector2 szf = strip.GetSize();
        int tw = (int)szf.X;
        int th = (int)szf.Y;
        if (tw <= 0 || th <= 0)
            return null;

        int cellW = tw / 12;
        if (cellW <= 0)
            return null;

        var atlases = new AtlasTexture[12];
        for (int i = 0; i < 12; i++)
        {
            int regionW = cellW * (i + 1);
            var atlas = new AtlasTexture
            {
                Atlas = strip,
                Region = new Rect2I(0, 0, regionW, th),
            };
            atlases[i] = atlas;
        }

        if (useFaceDownStrip)
            _atlasesByLevelFaceDown = atlases;
        else
            _atlasesByLevelFaceUp = atlases;

        return atlases;
    }

    private static void LayoutFieldIcon(
        Sprite2D portrait,
        string nodeName,
        Texture2D? tex,
        float iconH,
        float centerXFromPortraitCenter,
        float centerYFromPortraitCenter,
        int zIndex,
        float modulateAlpha)
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
        node.Modulate = new Color(1f, 1f, 1f, modulateAlpha);
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
        var node = EnsureSpriteChild(portrait, NodeEquip, 4);
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
        node.ZIndex = 4;
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
