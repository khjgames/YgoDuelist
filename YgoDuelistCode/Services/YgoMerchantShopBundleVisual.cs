using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Stacked <see cref="NCard"/> previews behind the main merchant offer for <see cref="YgoDuelistCard.BundledCards"/>.
/// </summary>
public static class YgoMerchantShopBundleVisual
{
    public const string BundleStackNodeName = "YgoShopBundleStack";
    private const string SigMetaKey = "YgoShopBundleSig";

    public static void MountOrRefresh(NMerchantCard slot, MerchantCardEntry entry)
    {
        Control? holder = slot.GetNodeOrNull<Control>("%CardHolder");
        if (holder == null)
            return;

        Control? existing = holder.GetNodeOrNull<Control>(BundleStackNodeName);

        if (entry.CreationResult?.Card == null
            || !YgoMerchantShopBundleShared.TryGetBundlingTemplate(entry.CreationResult.Card, out YgoDuelistCard y)
            || y.BundledCards.Length == 0)
        {
            existing?.QueueFree();
            return;
        }

        string sig = BuildSig(entry.CreationResult.Card, y);
        if (existing != null && existing.HasMeta(SigMetaKey) && existing.GetMeta(SigMetaKey).AsString() == sig)
            return;

        existing?.QueueFree();

        ModelId mainId = entry.CreationResult.Card.CanonicalInstance.Id;
        List<CardModel> previews = new();
        foreach (Type bt in y.BundledCards)
        {
            CardModel template;
            try
            {
                template = YgoPackCardCatalog.CardFromType(bt);
            }
            catch
            {
                continue;
            }

            if (template.Id == mainId && !y.BundleGrantsExtraCopyOfSelf)
                continue;

            previews.Add(template);
        }

        if (previews.Count == 0)
            return;

        var stack = new Control
        {
            Name = BundleStackNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        stack.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.OffsetLeft = 0;
        stack.OffsetTop = 0;
        stack.OffsetRight = 0;
        stack.OffsetBottom = 0;
        stack.SetMeta(SigMetaKey, sig);

        holder.AddChild(stack);
        holder.MoveChild(stack, 0);

        float scale = YgoMerchantShopLayoutTuning.MerchantBundlePreviewScale;
        Vector2 step = YgoMerchantShopLayoutTuning.MerchantBundlePreviewStepPixels;
        Vector2 origin = YgoMerchantShopLayoutTuning.MerchantBundlePreviewOriginPixels;

        for (int i = 0; i < previews.Count; i++)
        {
            NCard? nc = NCard.Create(previews[i]);
            if (nc == null)
                continue;
            nc.MouseFilter = Control.MouseFilterEnum.Ignore;
            nc.Scale = Vector2.One * scale;
            nc.Position = origin + step * (i + 1);
            nc.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            stack.AddChild(nc);
        }
    }

    private static string BuildSig(CardModel main, YgoDuelistCard y) =>
        $"{main.CanonicalInstance.Id}:{string.Join(",", y.BundledCards.Select(t => t.FullName))}";
}
