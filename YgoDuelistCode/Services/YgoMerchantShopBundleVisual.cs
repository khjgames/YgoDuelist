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
        CardModel? offer = entry.CreationResult?.Card;
        bool diag = YgoMerchantShopBundleDiag.IsMaskedBeastDiagCard(offer);

        if (holder == null)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("MountOrRefresh: %CardHolder missing");
            return;
        }

        Control? existing = holder.GetNodeOrNull<Control>(BundleStackNodeName);

        if (entry.CreationResult?.Card == null)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("MountOrRefresh: no bundle UI (CreationResult or Card null)");
            existing?.QueueFree();
            return;
        }

        CardModel offerCard = entry.CreationResult.Card;
        if (!YgoMerchantShopBundleShared.TryGetBundlingTemplate(offerCard, out YgoDuelistCard bundling))
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("MountOrRefresh: no bundle UI (TryGetBundlingTemplate false)");
            existing?.QueueFree();
            return;
        }

        if (bundling.BundledCards.Length == 0)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("MountOrRefresh: no bundle UI (BundledCards length 0 on template)");
            existing?.QueueFree();
            return;
        }

        string sig = BuildSig(offerCard, bundling);
        if (existing != null && existing.HasMeta(SigMetaKey) && existing.GetMeta(SigMetaKey).AsString() == sig)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log($"MountOrRefresh: sig unchanged skip rebuild sig={sig}");
            if (existing.GetIndex() != 0)
                holder.MoveChild(existing, 0);
            return;
        }

        if (diag)
            YgoMerchantShopBundleDiag.Log($"MountOrRefresh: rebuilding stack sig={sig}");

        existing?.QueueFree();

        ModelId mainId = offerCard.CanonicalInstance.Id;
        List<CardModel> previews = new();
        foreach (Type bt in bundling.BundledCards)
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

            if (template.Id == mainId && !bundling.BundleGrantsExtraCopyOfSelf)
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
        // First child = drawn underneath; main offer NCard stays last so it receives clicks and paints on top.
        holder.MoveChild(stack, 0);

        Vector2 previewScaleVec = ResolveMainOfferNCardScale(holder);
        Vector2 step = YgoMerchantShopLayoutTuning.MerchantBundlePreviewStepPixels;

        for (int i = 0; i < previews.Count; i++)
        {
            NCard? nc = NCard.Create(previews[i]);
            if (nc == null)
            {
                if (diag)
                    YgoMerchantShopBundleDiag.Log($"MountOrRefresh: NCard.Create null for {previews[i].Id.Entry}");
                continue;
            }
            nc.MouseFilter = Control.MouseFilterEnum.Ignore;
            nc.Scale = previewScaleVec;
            // NCard.UpdateVisuals no-ops until IsNodeReady(); must enter tree first or previews show as broken.
            stack.AddChild(nc);
        }

        ScheduleDeferredBundleLayout(holder, stack, previews.Count, previewScaleVec, step, diag);
    }

    private static void ScheduleDeferredBundleLayout(
        Control holder,
        Control stack,
        int previewCount,
        Vector2 previewScaleVec,
        Vector2 step,
        bool diag)
    {
        // Defer until after enter-tree / _ready so NCard.UpdateVisuals (requires IsNodeReady) runs reliably.
        holder.CallDeferred(
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(holder) || !GodotObject.IsInstanceValid(stack))
                    return;
                ApplyBundlePreviewLayout(holder, stack, previewCount, previewScaleVec, step, diag);
            }));
    }

    private static void ApplyBundlePreviewLayout(
        Control holder,
        Control stack,
        int previewCount,
        Vector2 previewScaleVec,
        Vector2 step,
        bool diag)
    {
        Vector2 sz = holder.Size;
        if (sz.X < 24f || sz.Y < 24f)
            sz = new Vector2(112f, 198f);

        Vector2 extra = YgoMerchantShopLayoutTuning.MerchantBundlePreviewOriginPixels;
        float fromBottom = YgoMerchantShopLayoutTuning.MerchantBundlePreviewAnchorFromBottomPx;
        Vector2 posNudge = YgoMerchantShopLayoutTuning.MerchantBundlePreviewPositionOffsetPixels;
        Vector2 basePos = new Vector2(4f + extra.X, sz.Y - fromBottom + extra.Y) + posNudge;

        Vector2 scaleVec = ResolveMainOfferNCardScale(holder);
        if (scaleVec.X <= 0f || scaleVec.Y <= 0f)
            scaleVec = previewScaleVec;

        int idx = 0;
        foreach (Node ch in stack.GetChildren())
        {
            if (ch is not NCard nc)
                continue;
            idx++;
            nc.Scale = scaleVec;
            nc.Position = basePos + step * idx;
            nc.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        }

        if (diag)
            YgoMerchantShopBundleDiag.Log(
                $"ApplyBundleLayout holderSize={holder.Size} szUsed={sz} base={basePos} scale={scaleVec} previews={previewCount}");
    }

    /// <summary>Match bundle preview size to the primary offer <see cref="NCard"/> in the same holder.</summary>
    private static Vector2 ResolveMainOfferNCardScale(Control holder)
    {
        float f = YgoMerchantShopLayoutTuning.MerchantBundlePreviewScaleFallback;
        Vector2 fallback = new(f, f);
        foreach (Node ch in holder.GetChildren())
        {
            if (ch.Name == BundleStackNodeName || ch is not NCard nc)
                continue;
            return nc.Scale;
        }

        return fallback;
    }

    private static string BuildSig(CardModel main, YgoDuelistCard y) =>
        $"{main.CanonicalInstance.Id}:{string.Join(",", y.BundledCards.Select(t => t.FullName))}";
}
