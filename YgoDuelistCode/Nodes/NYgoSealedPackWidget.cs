using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Sealed pack art (600×846). All PNGs live under <c>images/card_frames/Sealed_Cardpacks/</c>.
/// Outline layers use higher <see cref="CanvasItem.ZIndex"/>.
/// </summary>
public partial class NYgoSealedPackWidget : Button
{
    /// <summary>Relative to <see cref="StringExtensions.ImagePath"/> root (<c>YgoDuelist/images/</c>).</summary>
    private const string SealedDir = "card_frames/Sealed_Cardpacks";

    /// <summary>
    /// When <c>true</c>, tag tint layers use <c>shaders/ygo_cardpack_darken.gdshader</c> (per design doc).
    /// When <c>false</c>, tints use default canvas blending (<see cref="CanvasItemMaterial.BlendModeEnum.Mix"/>), no multiply.
    /// </summary>
    public static bool TagTintLayersUseDarkenShader = true;

    private static Shader? _darkenShaderCache;
    private static bool _darkenShaderLoadDone;

    private static Shader? _portraitMaskShaderCache;
    private static bool _portraitMaskShaderLoadDone;

    public const float PackDesignWidth = 600f;
    public const float PackDesignHeight = 846f;

    public const float PackDisplayWidth = 320f;

    public static float PackDisplayHeight => PackDisplayWidth * PackDesignHeight / PackDesignWidth;

    /// <summary>Selection glow quad width = pack width × this (centered on pack).</summary>
    public const float PackSelectionGlowScaleX = 2.29f;

    /// <summary>Selection glow quad height = pack height × this (centered on pack).</summary>
    public const float PackSelectionGlowScaleY = 1.75f;

    /// <summary>Draw selection glow under <c>PackArtStack</c> (stack uses z ≥ 0).</summary>
    public const int PackSelectionGlowZIndex = -1;

    private const int OutlineZBase = 10;

    /// <summary>Max blend toward tag darken tint; same as 142/255 in pack art spec.</summary>
    private const float TagTintLayerOpacity = 142f / 255f;

    private int _fillZ;
    private int _outlineZ;

    private TextureRect? _packBackgroundOutline;
    private Texture2D? _packBackgroundOutlineNormalTexture;
    private Texture2D? _packBackgroundOutlineSelectedTexture;

    private NCardHighlight? _selectionGlow;
    private ShaderMaterial? _glowShaderMaterial;
    private Tween? _glowTween;
    private float _glowWidthMul = 1f;
    private int _debugPackIndex;

    private static readonly StringName GlowWidthShaderParam = new("width");

    /// <summary>Vanilla highlight tweens this value on show (see <see cref="NCardHighlight.AnimShow"/>).</summary>
    private const float GlowWidthShownCard = 0.075f;

    /// <summary>Vanilla card scene; duplicate <c>%Highlight</c> for grid-style selection glow.</summary>
    private const string CardSceneResPath = "res://scenes/cards/card.tscn";

    private static string Png(string fileName) => $"{SealedDir}/{fileName}";

    public void Build(int packIndexForLog, YgoCardPackTags tagMask)
    {
        _debugPackIndex = packIndexForLog;
        Flat = true;
        AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        Text = string.Empty;
        var size = new Vector2(PackDisplayWidth, PackDisplayHeight);
        CustomMinimumSize = size;
        Size = size;

        _fillZ = 0;
        _outlineZ = OutlineZBase;

        var stack = new Control
        {
            Name = "PackArtStack",
            CustomMinimumSize = size,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(stack);
        stack.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        const string bgRelative = "Cardpack_Background.png";
        AddFillLayer(stack, Png(bgRelative), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        Texture2D? packBackground = PreloadManager.Cache.GetTexture2D(Png(bgRelative).ImagePath());
        if (packBackground == null)
            GD.PrintErr($"[YgoSealedPack] Pack background missing for darken shader: {bgRelative}");

        List<YgoCardPackTags> bits = YgoPackTagBits.EnumerateSingleBitsOrdered(tagMask);
        if (bits.Count == 0)
            bits.Add(YgoCardPackTags.Spell);

        if (bits.Count == 1)
        {
            AddTintLayer(stack, Png("Cardpack_S_Tag_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]), packBackground);
            AddPortraitSlots(stack, YgoSealedPackPortraitLayout.BuildSingleTagSlots(bits[0]));
        }
        else if (bits.Count == 2)
        {
            AddTintLayer(stack, Png("Cardpack_D_Tag_1_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]), packBackground);
            AddTintLayer(stack, Png("Cardpack_D_Tag_2_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[1]), packBackground);
            AddPortraitSlots(stack, YgoSealedPackPortraitLayout.BuildDoubleTagSlots(bits[0], bits[1]));
            AddOutlineLayer(stack, Png("Cardpack_D_Outline.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        }
        else
        {
            AddTintLayer(stack, Png("Cardpack_T_Tag_1_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]), packBackground);
            AddTintLayer(stack, Png("Cardpack_T_Tag_2_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[1]), packBackground);
            AddFillLayer(stack, Png("Cardpack_T_Rift.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
            AddPortraitSlots(stack, YgoSealedPackPortraitLayout.BuildTripleTagSlots(bits[0], bits[1], bits[2]));
            AddOutlineLayer(stack, Png("Cardpack_T_Outline.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        }

        const string bgOutlineNormal = "Cardpack_Background_Outline.png";
        const string bgOutlineSelected = "Cardpack_Background_Outline_Selected.png";
        _packBackgroundOutline = AddOutlineLayerAndReturn(stack, Png(bgOutlineNormal), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        _packBackgroundOutline.Name = "PackBackgroundOutline";
        _packBackgroundOutlineNormalTexture = _packBackgroundOutline.Texture;
        string selectedOutlinePath = Png(bgOutlineSelected).ImagePath();
        _packBackgroundOutlineSelectedTexture = PreloadManager.Cache.GetTexture2D(selectedOutlinePath);
        if (_packBackgroundOutlineSelectedTexture == null)
            GD.PrintErr($"[YgoSealedPack] Missing texture: {bgOutlineSelected} -> {selectedOutlinePath}");

        AttachSelectionGlow();
    }

    /// <summary>Same cyan edge treatment as <see cref="NCardGrid.HighlightCard"/> via <see cref="NCardHighlight"/>.</summary>
    public void SetPackSelectionFocused(bool focused)
    {
        ApplyPackBackgroundOutlineSelection(focused);

        if (focused)
            Modulate = Colors.White;
        else
            Modulate = new Color(0.55f, 0.55f, 0.55f, 1f);

        if (_selectionGlow == null || _glowShaderMaterial == null)
            return;

        if (focused)
        {
            _selectionGlow.Modulate = NCardHighlight.playableColor;
            PackGlowAnimShow();
        }
        else
            PackGlowAnimHide();
    }

    private void ApplyPackBackgroundOutlineSelection(bool selected)
    {
        if (_packBackgroundOutline == null)
            return;
        _packBackgroundOutline.Texture = selected && _packBackgroundOutlineSelectedTexture != null
            ? _packBackgroundOutlineSelectedTexture
            : _packBackgroundOutlineNormalTexture;
    }

    private void AttachSelectionGlow()
    {
        try
        {
            var cardRoot = PreloadManager.Cache.GetScene(CardSceneResPath).Instantiate<Control>(PackedScene.GenEditState.Disabled);
            var hl = cardRoot.GetNodeOrNull<NCardHighlight>("%Highlight");
            if (hl == null)
            {
                cardRoot.QueueFree();
                return;
            }

            _selectionGlow = (NCardHighlight)hl.Duplicate();
            cardRoot.QueueFree();
            _selectionGlow.Name = "PackSelectionGlow";
            _selectionGlow.MouseFilter = Control.MouseFilterEnum.Ignore;
            _selectionGlow.Modulate = NCardHighlight.playableColor;
            _selectionGlow.StretchMode = TextureRect.StretchModeEnum.Scale;
            _selectionGlow.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _selectionGlow.ZIndex = PackSelectionGlowZIndex;
            if (_selectionGlow.Material is ShaderMaterial sm)
            {
                _glowShaderMaterial = (ShaderMaterial)sm.Duplicate();
                _selectionGlow.Material = _glowShaderMaterial;
            }
            else
                GD.PrintErr("[YgoSealedPack] PackSelectionGlow: highlight has no ShaderMaterial; glow width scaling disabled.");

            AddChild(_selectionGlow);
            MoveChild(_selectionGlow, 0);
            Callable.From(DeferredLayoutSelectionGlowAndHide).CallDeferred();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoSealedPack] Pack selection glow: {ex.Message}");
        }
    }

    /// <summary>Fill the pack button; shader <c>width</c> is tuned for card pixels — scale by max edge ratio vs <see cref="NCard.defaultSize"/>.</summary>
    private void DeferredLayoutSelectionGlowAndHide()
    {
        if (_selectionGlow == null || !GodotObject.IsInstanceValid(_selectionGlow))
            return;

        ClipContents = false;

        Vector2 pack = Size;
        if (pack.X < 2f || pack.Y < 2f)
            pack = new Vector2(PackDisplayWidth, PackDisplayHeight);

        Vector2 refCard = NCard.defaultSize;
        float baseMul = Mathf.Max(pack.X / refCard.X, pack.Y / refCard.Y);
        float glowScaleMax = Mathf.Max(PackSelectionGlowScaleX, PackSelectionGlowScaleY);
        _glowWidthMul = baseMul * glowScaleMax;

        Vector2 glowSize = new(pack.X * PackSelectionGlowScaleX, pack.Y * PackSelectionGlowScaleY);
        _selectionGlow.LayoutMode = 0;
        _selectionGlow.SetAnchorsPreset(LayoutPreset.TopLeft);
        _selectionGlow.Position = new Vector2(
            pack.X * (1f - PackSelectionGlowScaleX) * 0.5f,
            pack.Y * (1f - PackSelectionGlowScaleY) * 0.5f);
        _selectionGlow.Size = glowSize;
        _selectionGlow.Scale = Vector2.One;
        _selectionGlow.PivotOffset = Vector2.Zero;

        PackGlowSetWidth(0f);

        Rect2 btnG = GetGlobalRect();
        Rect2 glowG = _selectionGlow.GetGlobalRect();
        GD.PrintErr(
            $"[YgoSealedPack] glow layout packIdx={_debugPackIndex} btn size={Size} min={CustomMinimumSize} packEff={pack} " +
            $"glowScaleXY=({PackSelectionGlowScaleX},{PackSelectionGlowScaleY}) refCard={refCard} widthMul={_glowWidthMul:F4} " +
            $"glow local size={_selectionGlow.Size} scale={_selectionGlow.Scale} " +
            $"btn_global={btnG.Position} {btnG.Size} glow_global={glowG.Position} {glowG.Size}");
    }

    private void PackGlowSetWidth(float w) => _glowShaderMaterial?.SetShaderParameter(GlowWidthShaderParam, w);

    private void PackGlowAnimShow()
    {
        if (_selectionGlow == null || _glowShaderMaterial == null)
            return;
        _glowTween?.Kill();
        float start = _glowShaderMaterial.GetShaderParameter(GlowWidthShaderParam).AsSingle();
        float end = GlowWidthShownCard * _glowWidthMul;
        _glowTween = _selectionGlow.CreateTween();
        _glowTween
            .TweenMethod(Callable.From<float>(PackGlowSetWidth), start, end, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void PackGlowAnimHide()
    {
        if (_selectionGlow == null || _glowShaderMaterial == null)
            return;
        _glowTween?.Kill();
        float start = _glowShaderMaterial.GetShaderParameter(GlowWidthShaderParam).AsSingle();
        _glowTween = _selectionGlow.CreateTween();
        _glowTween.TweenMethod(Callable.From<float>(PackGlowSetWidth), start, 0f, 0.5);
    }

    private void AddPortraitSlots(Control stack, IReadOnlyList<SealedPackPortraitSlot> slots)
    {
        float s = PackDisplayWidth / PackDesignWidth;
        Shader? maskShader = TryGetPortraitMaskShader();

        foreach (SealedPackPortraitSlot slot in slots)
        {
            var box = new Control
            {
                Name = $"PortraitSlot_{slot.PortraitFileName.Replace('.', '_')}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                LayoutMode = 0,
                Position = new Vector2(slot.DesignPosition.X * s, slot.DesignPosition.Y * s),
                Size = new Vector2(slot.DesignSize.X * s, slot.DesignSize.Y * s)
            };
            box.CustomMinimumSize = box.Size;
            stack.AddChild(box);
            box.ZIndex = _fillZ++;

            // Pack art uses filenames as authored under Sealed_Cardpacks/Tag_Portraits/ (e.g. xyz-dragon_cannon.png).
            string tagPortraitPath = Png($"Tag_Portraits/{slot.PortraitFileName}").ImagePath();
            Texture2D? portraitTex = PreloadManager.Cache.GetTexture2D(tagPortraitPath);
            if (portraitTex == null)
            {
                string cardPortraitPath = YgoPackTagVisualDefaults.NormalizeTagPortraitFileName(slot.PortraitFileName).CardImagePath();
                portraitTex = PreloadManager.Cache.GetTexture2D(cardPortraitPath);
            }

            string maskRel = slot.Template == SealedPackPortraitTemplate.A
                ? Png("Portrait_Mask_A.png")
                : Png("Portrait_Mask_B.png");
            string outlineRel = slot.Template == SealedPackPortraitTemplate.A
                ? Png("Portrait_Outline_A.png")
                : Png("Portrait_Outline_B.png");

            Texture2D? maskTex = PreloadManager.Cache.GetTexture2D(maskRel.ImagePath());
            Texture2D? outlineTex = PreloadManager.Cache.GetTexture2D(outlineRel.ImagePath());

            if (portraitTex == null)
            {
                GD.PrintErr(
                    $"[YgoSealedPack] Missing portrait for '{slot.PortraitFileName}': tried {tagPortraitPath} then " +
                    $"{YgoPackTagVisualDefaults.NormalizeTagPortraitFileName(slot.PortraitFileName).CardImagePath()}");
            }
            if (maskTex == null)
                GD.PrintErr($"[YgoSealedPack] Missing portrait mask: {maskRel}");
            if (outlineTex == null)
                GD.PrintErr($"[YgoSealedPack] Missing portrait outline: {outlineRel}");

            var portraitTr = new TextureRect
            {
                Name = "PortraitArt",
                Texture = portraitTex,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = Colors.White,
                ZIndex = 0
            };
            portraitTr.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            box.AddChild(portraitTr);

            if (maskShader != null && maskTex != null && portraitTex != null)
            {
                var mat = new ShaderMaterial { Shader = maskShader };
                mat.SetShaderParameter("mask_texture", maskTex);
                portraitTr.Material = mat;
            }
            else
                portraitTr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };

            var outlineTr = new TextureRect
            {
                Name = "PortraitOutline",
                Texture = outlineTex,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = Colors.White,
                ZIndex = 1
            };
            outlineTr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };
            outlineTr.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            box.AddChild(outlineTr);
        }
    }

    private void AddFillLayer(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend) =>
        AddTextureLayer(parent, relativeImage, modulate, blend, asOutline: false);

    private void AddOutlineLayer(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend) =>
        AddTextureLayer(parent, relativeImage, modulate, blend, asOutline: true);

    private TextureRect AddOutlineLayerAndReturn(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend)
    {
        TextureRect tr = NewTextureRectCore(parent, relativeImage, modulate);
        tr.Material = new CanvasItemMaterial { BlendMode = blend };
        tr.ZIndex = _outlineZ++;
        return tr;
    }

    /// <summary>Tag tint: Mix + modulate when shader off; darken shader samples <paramref name="packBackground"/> only.</summary>
    private void AddTintLayer(Control parent, string relativeImage, Color rgb, Texture2D? packBackground)
    {
        TextureRect tr = NewTextureRectCore(parent, relativeImage, Colors.White);
        if (TagTintLayersUseDarkenShader)
        {
            Shader? shader = TryGetDarkenShader();
            if (shader != null && packBackground != null && tr.Texture != null)
            {
                var mat = new ShaderMaterial { Shader = shader };
                mat.SetShaderParameter("background_texture", packBackground);
                mat.SetShaderParameter("mask_texture", tr.Texture);
                mat.SetShaderParameter("layer_opacity", TagTintLayerOpacity);
                mat.SetShaderParameter("tint_color", new Color(rgb.R, rgb.G, rgb.B, 1f));
                tr.Material = mat;
                // Modulate must be white: engine multiplies COLOR after fragment; tag tint is tint_color uniform only.
                tr.Modulate = Colors.White;
            }
            else
            {
                tr.Modulate = new Color(rgb.R, rgb.G, rgb.B, 1f);
                tr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };
            }
        }
        else
        {
            tr.Modulate = new Color(rgb.R, rgb.G, rgb.B, 1f);
            tr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };
        }

        tr.ZIndex = _fillZ++;
    }

    private void AddTextureLayer(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend, bool asOutline)
    {
        TextureRect tr = NewTextureRectCore(parent, relativeImage, modulate);
        tr.Material = new CanvasItemMaterial { BlendMode = blend };
        tr.ZIndex = asOutline ? _outlineZ++ : _fillZ++;
    }

    private static TextureRect NewTextureRectCore(Control parent, string relativeImage, Color modulate)
    {
        string path = relativeImage.ImagePath();
        Texture2D? tex = PreloadManager.Cache.GetTexture2D(path);
        if (tex == null)
            GD.PrintErr($"[YgoSealedPack] Missing texture: {relativeImage} -> {path}");

        var tr = new TextureRect
        {
            Name = relativeImage.Replace('/', '_').Replace('.', '_'),
            Texture = tex,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Modulate = modulate,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        tr.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        parent.AddChild(tr);
        return tr;
    }

    private static Shader? TryGetDarkenShader()
    {
        if (_darkenShaderLoadDone)
            return _darkenShaderCache;

        _darkenShaderLoadDone = true;
        string res = $"res://{MainFile.ModId}/shaders/ygo_cardpack_darken.gdshader";
        _darkenShaderCache = ResourceLoader.Load<Shader>(res, null, ResourceLoader.CacheMode.Reuse);
        if (_darkenShaderCache == null)
            GD.PrintErr($"[YgoSealedPack] TagTintLayersUseDarkenShader is true but shader not found: {res}");

        return _darkenShaderCache;
    }

    private static Shader? TryGetPortraitMaskShader()
    {
        if (_portraitMaskShaderLoadDone)
            return _portraitMaskShaderCache;

        _portraitMaskShaderLoadDone = true;
        string res = $"res://{MainFile.ModId}/shaders/ygo_sealed_portrait_mask.gdshader";
        _portraitMaskShaderCache = ResourceLoader.Load<Shader>(res, null, ResourceLoader.CacheMode.Reuse);
        if (_portraitMaskShaderCache == null)
            GD.PrintErr($"[YgoSealedPack] Portrait mask shader not found: {res}");

        return _portraitMaskShaderCache;
    }
}
