using System;
using Godot;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Loads YGO frame PNGs from <c>YgoDuelist/images/card_frames/</c> and scales them to 5% for card-library tickbox icons.</summary>
internal static class YgoCardLibraryYgoCardTypeFilterIcons
{
    internal static readonly Lazy<Texture2D> Any =
        new(() => LoadScaledIcon("card_frames/ygo_any.png"));

    internal static readonly Lazy<Texture2D> NormalMonster =
        new(() => LoadScaledIcon("card_frames/ygo_monster.png"));

    internal static readonly Lazy<Texture2D> EffectMonster =
        new(() => LoadScaledIcon("card_frames/ygo_effect_monster.png"));

    internal static readonly Lazy<Texture2D> Trap =
        new(() => LoadScaledIcon("card_frames/ygo_trap.png"));

    internal static readonly Lazy<Texture2D> Spell =
        new(() => LoadScaledIcon("card_frames/ygo_spell.png"));

    internal static readonly Lazy<Texture2D> FusionMonster =
        new(() => LoadScaledIcon("card_frames/ygo_fusion_monster.png"));

    internal static readonly Lazy<Texture2D> RitualMonster =
        new(() => LoadScaledIcon("card_frames/ygo_ritual_monster.png"));

    static Texture2D LoadScaledIcon(string fileUnderImages)
    {
        string rel = fileUnderImages.ImagePath().Replace('\\', '/');
        string res = rel.StartsWith("res://", StringComparison.Ordinal) ? rel : "res://" + rel;
        Texture2D? tex = ResourceLoader.Load<Texture2D>(res, null, ResourceLoader.CacheMode.Reuse);
        if (tex == null)
            throw new InvalidOperationException($"YgoDuelist: missing card library YGO type filter icon: {res}");
        return ScaleTextureToFivePercent(tex);
    }

    static Texture2D ScaleTextureToFivePercent(Texture2D tex)
    {
        Image? img = tex.GetImage();
        if (img == null)
            throw new InvalidOperationException("YgoDuelist: YGO type filter icon has no CPU image data.");

        Image copy = (Image)img.Duplicate();
        int nw = Math.Max(1, (int)(copy.GetWidth() * 0.05f));
        int nh = Math.Max(1, (int)(copy.GetHeight() * 0.05f));
        copy.Resize(nw, nh, Image.Interpolation.Lanczos);
        return ImageTexture.CreateFromImage(copy);
    }
}
