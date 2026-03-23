using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Applies set/face-down tint to portrait border and type plaque; offsets plaque Y when set frame is active.
/// Title banner modulate matches the type plaque after each tint apply; <see cref="NCard.UpdateTypePlaqueSizeAndPosition"/> postfix re-syncs banner from plaque (deferred layout) without overwriting plaque modulate.
/// </summary>
[HarmonyPatch]
public static class YgoSetModePlaqueAndFrameTintPatch
{
    private const float TypePlaqueSetFrameYOffset = 22f;
    private const float VanillaTypePlaqueXMargin = 17f;
    private const float VanillaTypePlaqueMinXSize = 61f;
    private const string FaceDownPortraitPath = "YgoDuelist/images/card_portraits/Face_Down_2.png";
    private const string FaceDownPortraitOverlayNodeName = "YgoFaceDownPortraitOverlay";

    private static readonly StringName DefaultPortraitBorderTintMeta = new("YgoDefaultPortraitBorderTint");
    private static readonly StringName DefaultTypePlaqueTintMeta = new("YgoDefaultTypePlaqueTint");
    private static readonly StringName BaseTypePlaqueYMeta = new("YgoBaseTypePlaqueY");
    private static Texture2D? _faceDownPortraitOverlayTexture;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter("YgoDuelist.YgoDuelistCode.Patches.YgoMonsterLevelStripPatch", "YgoDuelist.YgoDuelistCode.Patches.YgoSpellTrapRaceIconPatch")]
    public static void ReloadPostfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        var model = __instance.Model;
        var body = __instance.Body;
        if (model == null || body == null)
            return;

        bool useSetVisual = YgoSetCardVisualHelper.ShouldUseSetFrame(model);

        TextureRect? portrait = body.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null)
        {
            TextureRect? overlay = EnsurePortraitOverlayNode(body, portrait);
            if (overlay == null)
                return;
            SyncOverlayToPortrait(overlay, portrait);

            _faceDownPortraitOverlayTexture ??= ResourceLoader.Load<Texture2D>(FaceDownPortraitPath, null, ResourceLoader.CacheMode.Reuse);
            overlay.Texture = _faceDownPortraitOverlayTexture;
            overlay.Visible = useSetVisual && portrait.Visible && _faceDownPortraitOverlayTexture != null;
        }

        ApplyPortraitBorderTypePlaqueAndBannerModulate(model, body);

        NinePatchRect? typePlaque = body.GetNodeOrNull<NinePatchRect>("%TypePlaque");
        if (typePlaque != null && !typePlaque.HasMeta(BaseTypePlaqueYMeta))
            typePlaque.SetMeta(BaseTypePlaqueYMeta, typePlaque.Position.Y);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateTypePlaqueSizeAndPosition")]
    [HarmonyPriority(Priority.Last)]
    public static void TypePlaquePositionPostfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        var model = __instance.Model;
        var body = __instance.Body;
        if (model == null || body == null)
            return;

        NinePatchRect? typePlaque = body.GetNodeOrNull<NinePatchRect>("%TypePlaque");
        Control? typeLabel = body.GetNodeOrNull<Control>("%TypeLabel");
        if (typePlaque == null)
            return;
        if (typeLabel == null)
            return;

        if (!typePlaque.HasMeta(BaseTypePlaqueYMeta))
            typePlaque.SetMeta(BaseTypePlaqueYMeta, typePlaque.Position.Y);

        bool useSetFrame = model.Type == CardType.Skill
                           && YgoSetCardVisualHelper.ShouldUseSetFrame(model);

        // Re-apply vanilla plaque layout math every invocation so positioning is deterministic.
        float centerX = typePlaque.Position.X + typePlaque.Size.X * 0.5f;
        Vector2 plaqueSize = typePlaque.Size;
        plaqueSize.X = Mathf.Max(typeLabel.Size.X + VanillaTypePlaqueXMargin, VanillaTypePlaqueMinXSize);
        typePlaque.Size = plaqueSize;

        float baseY = ExtractFloat(typePlaque.GetMeta(BaseTypePlaqueYMeta, typePlaque.Position.Y), typePlaque.Position.Y);
        float newX = centerX - typePlaque.Size.X * 0.5f;
        float scaledOffset = TypePlaqueSetFrameYOffset * typePlaque.Scale.Y;
        float newY = useSetFrame ? (baseY - scaledOffset) : baseY;
        typePlaque.Position = new Vector2(newX, newY);

        // Don’t re-apply plaque modulate here — vanilla may have set rarity tint after Reload. Only mirror plaque → banner.
        SyncTitleBannerModulateFromTypePlaque(body);
    }

    /// <summary>
    /// Keeps portrait border + type plaque in sync with set mode; then sets title banner modulate to match the plaque.
    /// </summary>
    private static void ApplyPortraitBorderTypePlaqueAndBannerModulate(CardModel model, Control body)
    {
        TextureRect? portraitBorder = body.GetNodeOrNull<TextureRect>("%PortraitBorder");
        NinePatchRect? typePlaque = body.GetNodeOrNull<NinePatchRect>("%TypePlaque");
        if (portraitBorder == null || typePlaque == null)
            return;

        bool useSetVisual = YgoSetCardVisualHelper.ShouldUseSetFrame(model);

        if (!portraitBorder.HasMeta(DefaultPortraitBorderTintMeta))
            portraitBorder.SetMeta(DefaultPortraitBorderTintMeta, portraitBorder.Modulate);
        if (!typePlaque.HasMeta(DefaultTypePlaqueTintMeta))
            typePlaque.SetMeta(DefaultTypePlaqueTintMeta, typePlaque.Modulate);

        Color defaultPortraitTint = ExtractColor(portraitBorder.GetMeta(DefaultPortraitBorderTintMeta, portraitBorder.Modulate), portraitBorder.Modulate);
        Color defaultTypePlaqueTint = ExtractColor(typePlaque.GetMeta(DefaultTypePlaqueTintMeta, typePlaque.Modulate), typePlaque.Modulate);

        bool useSetFrame = model.Type == CardType.Skill && useSetVisual;

        portraitBorder.Modulate = useSetFrame ? YgoSetCardVisualHelper.SetOrFaceDownTint : defaultPortraitTint;
        typePlaque.Modulate = useSetFrame ? YgoSetCardVisualHelper.SetOrFaceDownTint : defaultTypePlaqueTint;

        SyncTitleBannerModulateFromTypePlaque(body);
    }

    private static void SyncTitleBannerModulateFromTypePlaque(Control body)
    {
        NinePatchRect? typePlaque = body.GetNodeOrNull<NinePatchRect>("%TypePlaque");
        TextureRect? titleBanner = body.GetNodeOrNull<TextureRect>("%TitleBanner");
        if (typePlaque == null || titleBanner == null)
            return;
        titleBanner.Modulate = typePlaque.Modulate;
    }

    private static Color ExtractColor(Variant variant, Color fallback)
    {
        return variant.VariantType == Variant.Type.Color ? variant.AsColor() : fallback;
    }

    private static float ExtractFloat(Variant variant, float fallback)
    {
        return variant.VariantType == Variant.Type.Float ? (float)variant : fallback;
    }

    private static TextureRect? EnsurePortraitOverlayNode(Control body, TextureRect portrait)
    {
        Node? portraitParentNode = portrait.GetParent();
        if (portraitParentNode == null)
            return null;
        Node portraitParent = portraitParentNode;

        // If an older overlay exists under the wrong parent, remove it so we can recreate correctly.
        TextureRect? staleInBody = body.GetNodeOrNull<TextureRect>(FaceDownPortraitOverlayNodeName);
        if (staleInBody != null && staleInBody.GetParent() != portraitParent)
        {
            staleInBody.GetParent()?.RemoveChild(staleInBody);
            staleInBody.QueueFree();
        }

        TextureRect? existing = portraitParent.GetNodeOrNull<TextureRect>(FaceDownPortraitOverlayNodeName);
        if (existing != null)
        {
            portraitParent.MoveChild(existing, portrait.GetIndex() + 1);
            return existing;
        }

        var overlay = new TextureRect
        {
            Name = FaceDownPortraitOverlayNodeName,
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
