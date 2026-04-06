using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
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
    public static bool TagTintLayersUseDarkenShader;

    private static Shader? _darkenShaderCache;
    private static bool _darkenShaderLoadDone;

    public const float PackDesignWidth = 600f;
    public const float PackDesignHeight = 846f;

    public const float PackDisplayWidth = 320f;

    public static float PackDisplayHeight => PackDisplayWidth * PackDesignHeight / PackDesignWidth;

    private const int OutlineZBase = 10;

    private int _fillZ;
    private int _outlineZ;
    private int _debugPackIndex;
    private Control? _debugStack;

    private static string Png(string fileName) => $"{SealedDir}/{fileName}";

    public void Build(int packIndexForLog, YgoCardPackTags tagMask)
    {
        GD.PrintErr($"[YgoSealedPack] Build pack[{packIndexForLog}] mask={tagMask}");
        Flat = true;
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

        AddFillLayer(stack, Png("Cardpack_Background.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);

        List<YgoCardPackTags> bits = YgoPackTagBits.EnumerateSingleBitsOrdered(tagMask);
        if (bits.Count == 0)
            bits.Add(YgoCardPackTags.Spell);

        if (bits.Count == 1)
            AddTintLayer(stack, Png("Cardpack_S_Tag_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]));
        else if (bits.Count == 2)
        {
            AddTintLayer(stack, Png("Cardpack_D_Tag_1_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]));
            AddTintLayer(stack, Png("Cardpack_D_Tag_2_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[1]));
            AddOutlineLayer(stack, Png("Cardpack_D_Outline.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        }
        else
        {
            AddTintLayer(stack, Png("Cardpack_T_Tag_1_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[0]));
            AddTintLayer(stack, Png("Cardpack_T_Tag_2_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[1]));
            AddTintLayer(stack, Png("Cardpack_T_Tag_3_Darken_Tint.png"), YgoPackTagVisualDefaults.GetTintColor(bits[2]));
            AddFillLayer(stack, Png("Cardpack_T_Rift.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
            AddOutlineLayer(stack, Png("Cardpack_T_Outline.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);
        }

        AddOutlineLayer(stack, Png("Cardpack_Background_Outline.png"), Colors.White, CanvasItemMaterial.BlendModeEnum.Mix);

        _debugPackIndex = packIndexForLog;
        _debugStack = stack;
        CallDeferred(nameof(DeferredDebugPrintPackLayers));
    }

    private void DeferredDebugPrintPackLayers()
    {
        if (_debugStack != null)
            DebugPrintPackLayers(_debugPackIndex, this, _debugStack);
    }

    private void AddFillLayer(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend) =>
        AddTextureLayer(parent, relativeImage, modulate, blend, asOutline: false);

    private void AddOutlineLayer(Control parent, string relativeImage, Color modulate, CanvasItemMaterial.BlendModeEnum blend) =>
        AddTextureLayer(parent, relativeImage, modulate, blend, asOutline: true);

    /// <summary>Tag tint: Mix + modulate when shader off; darken shader when on.</summary>
    private void AddTintLayer(Control parent, string relativeImage, Color rgb)
    {
        TextureRect tr = NewTextureRectCore(parent, relativeImage, Colors.White);
        tr.Modulate = new Color(rgb.R, rgb.G, rgb.B, 1f);
        if (TagTintLayersUseDarkenShader)
        {
            Shader? shader = TryGetDarkenShader();
            if (shader != null)
                tr.Material = new ShaderMaterial { Shader = shader };
            else
                tr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };
        }
        else
            tr.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };

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

    private static void DebugPrintPackLayers(int packIndex, NYgoSealedPackWidget button, Control stack)
    {
        if (!GodotObject.IsInstanceValid(button) || !GodotObject.IsInstanceValid(stack))
            return;

        Rect2 g = button.GetGlobalRect();
        GD.PrintErr(
            $"[YgoSealedPack] pack[{packIndex}] button global_rect={g.Position} size={g.Size} " +
            $"min={button.CustomMinimumSize} z={button.ZIndex}");

        foreach (Node child in stack.GetChildren())
        {
            if (child is not TextureRect tr)
                continue;
            Rect2 tg = tr.GetGlobalRect();
            GD.PrintErr(
                $"[YgoSealedPack] pack[{packIndex}] layer '{tr.Name}' z={tr.ZIndex} " +
                $"global_rect={tg.Position} size={tg.Size} tex={(tr.Texture != null ? "ok" : "NULL")}");
        }
    }
}
