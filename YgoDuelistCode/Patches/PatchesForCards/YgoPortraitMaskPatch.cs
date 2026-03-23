using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Applies a grayscale mask shader to <c>%Portrait</c> for YGO cards (white = visible, black = hidden).
/// Masks match portrait resolution; swapped on <see cref="NCard.Reload"/> when set/attack/skill state changes.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoPortraitMaskPatch
{
    private const string ShaderResourcePath = "YgoDuelist/shaders/ygo_portrait_mask.gdshader";

    private const string MaskAttackPath = "YgoDuelist/images/card_portraits/masks/portrait_mask_attack.png";
    private const string MaskSetPath = "YgoDuelist/images/card_portraits/masks/portrait_mask_set.png";
    private const string MaskSkillPath = "YgoDuelist/images/card_portraits/masks/portrait_mask_skill.png";

    private static Shader? _shader;
    private static readonly object ShaderLoadLock = new();

    private static Texture2D? _maskAttack;
    private static Texture2D? _maskSet;
    private static Texture2D? _maskSkill;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || !__instance.IsNodeReady())
            return;

        CardModel? model = __instance.Model;
        Control body = __instance.Body;
        var portrait = body.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait == null || model == null)
            return;

        if (model.Rarity == CardRarity.Ancient)
            return;

        if (__instance.Visibility != ModelVisibility.Visible)
            return;

        if (!YgoPortraitMaskResolver.ShouldApplyPortraitMask(model))
            return;

        Shader? shader = EnsureShader();
        if (shader == null)
            return;

        YgoPortraitMaskKind kind = YgoPortraitMaskResolver.Resolve(model);
        Texture2D? maskTex = GetMaskTexture(kind);
        if (maskTex == null)
            return;

        if (portrait.Material is ShaderMaterial existing && existing.Shader == shader)
        {
            existing.SetShaderParameter("mask_tex", maskTex);
            return;
        }

        var mat = new ShaderMaterial { Shader = shader };
        mat.SetShaderParameter("mask_tex", maskTex);
        portrait.Material = mat;
    }

    private static Shader? EnsureShader()
    {
        if (_shader != null)
            return _shader;
        lock (ShaderLoadLock)
        {
            if (_shader != null)
                return _shader;
            if (!ResourceLoader.Exists(ShaderResourcePath))
                return null;
            _shader = ResourceLoader.Load<Shader>(ShaderResourcePath, null, ResourceLoader.CacheMode.Reuse);
            return _shader;
        }
    }

    private static Texture2D? GetMaskTexture(YgoPortraitMaskKind kind)
    {
        string path = kind switch
        {
            YgoPortraitMaskKind.Set => MaskSetPath,
            YgoPortraitMaskKind.Attack => MaskAttackPath,
            _ => MaskSkillPath,
        };

        return kind switch
        {
            YgoPortraitMaskKind.Set => LoadMask(ref _maskSet, path),
            YgoPortraitMaskKind.Attack => LoadMask(ref _maskAttack, path),
            _ => LoadMask(ref _maskSkill, path),
        };
    }

    private static Texture2D? LoadMask(ref Texture2D? cache, string path)
    {
        if (cache != null)
            return cache;
        if (!ResourceLoader.Exists(path))
            return null;
        cache = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        return cache;
    }
}
