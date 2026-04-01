using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Set-frame skill presentation swaps <see cref="CardModel.PortraitBorder"/> to <c>Inverted_Lip_Set</c>; the portrait
/// canvas group must use the engine mask materials. Grid/deck holders scale the card after <see cref="NCard.Reload"/>,
/// so we re-apply once deferred and again after plaque layout settles.
/// </summary>
internal static class YgoSetFramePortraitCanvasMask
{
    private const string CanvasGroupMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";
    private const string CanvasGroupMaskBlurMaterialPath = "res://scenes/cards/card_canvas_group_mask_blur_material.tres";

    public static void Apply(NCard nCard)
    {
        if (nCard == null || !nCard.IsNodeReady())
            return;
        var model = nCard.Model;
        var body = nCard.Body;
        if (model == null || body == null)
            return;
        ApplyCore(nCard, model, body);
    }

    public static void ApplyDeferred(NCard nCard)
    {
        NCard captured = nCard;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(captured))
                return;
            Apply(captured);
        }).CallDeferred();
    }

    private static void ApplyCore(NCard nCard, CardModel model, Control body)
    {
        if (model.Rarity == CardRarity.Ancient)
            return;
        if (model.Type != CardType.Skill || !YgoSetCardVisualHelper.ShouldUseSetFrame(model))
            return;

        TextureRect? portraitBorder = body.GetNodeOrNull<TextureRect>("%PortraitBorder");
        if (portraitBorder != null)
            portraitBorder.Texture = model.PortraitBorder;

        CanvasGroup? portraitGroup = body.GetNodeOrNull<CanvasGroup>("%PortraitCanvasGroup");
        if (portraitGroup == null)
            return;

        string path = nCard.Visibility != ModelVisibility.Visible
            ? CanvasGroupMaskBlurMaterialPath
            : CanvasGroupMaskMaterialPath;
        portraitGroup.Material = PreloadManager.Cache.GetMaterial(path);
    }
}
