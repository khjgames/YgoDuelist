using System.Text;
using System.Threading;
using Godot;
using YgoDuelist;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Verbose scale tracing for the YGO merchant buy grid (grep <c>[YgoDuelist][YgoBuyScale]</c> in godot.log).
/// </summary>
public static class YgoMerchantBuyGridScaleTrace
{
    public const string Tag = "[YgoDuelist][YgoBuyScale]";

    private static int _seq;

    public static void Log(string message) =>
        MainFile.Logger.Info($"{Tag} #{Interlocked.Increment(ref _seq)} {message}");

    /// <summary>True when <paramref name="slot"/> is a descendant of a node named <c>YgoAddonBuyGrid</c>.</summary>
    public static bool IsYgoBuyGridSlot(NMerchantSlot slot)
    {
        for (Node? n = slot.GetParent(); n != null; n = n.GetParent())
        {
            if (n.Name == "YgoAddonBuyGrid")
                return true;
        }

        return false;
    }

    public static GridContainer? FindYgoBuyGrid(NMerchantSlot slot)
    {
        for (Node? n = slot.GetParent(); n != null; n = n.GetParent())
        {
            if (n is GridContainer g && n.Name == "YgoAddonBuyGrid")
                return g;
        }

        return null;
    }

    public static string SlotOneLine(string phase, NMerchantSlot slot)
    {
        if (!GodotObject.IsInstanceValid(slot))
            return $"{phase} slot=(invalid)";

        string offer = "?";
        try
        {
            if (slot is NMerchantCard mc && mc.Entry is MerchantCardEntry mce)
                offer = mce.CreationResult?.Card?.Id.Entry ?? "(empty)";
        }
        catch
        {
            offer = "(entry-ex)";
        }

        Transform2D gt = slot.GetGlobalTransform();
        NCard? main = TryFindMainOfferNCard(slot);
        Vector2 nCardScale = main != null && GodotObject.IsInstanceValid(main) ? main.Scale : new Vector2(-1f, -1f);
        Vector2 chromeScale;
        if (slot is NMerchantCard mcForRoot && YgoBuyGridMerchantSlotChromeScale.GetScaleRoot(mcForRoot) is { } scaleRoot)
            chromeScale = scaleRoot.Scale;
        else if (slot is NMerchantCard mcForHolder && mcForHolder.GetNodeOrNull<Control>("%CardHolder") is { } holder)
            chromeScale = holder.Scale;
        else
            chromeScale = slot.Scale;
        return
            $"{phase} path={slot.GetPath()} offer={offer} slotScale={slot.Scale} buyGridChromeScale={chromeScale} globalXformScale={gt.Scale} mainNCardScale={nCardScale}";
    }

    public static void DumpEntireGrid(string reason, GridContainer? grid)
    {
        if (grid == null || !GodotObject.IsInstanceValid(grid))
        {
            Log($"{reason} DumpGrid: grid null or freed");
            return;
        }

        Control? p = grid.GetParent() as Control;
        Transform2D gg = grid.GetGlobalTransform();
        Log($"{reason} DumpGrid: grid path={grid.GetPath()} grid.Scale={grid.Scale} grid.globalXformScale={gg.Scale} parent={(p == null ? "null" : $"{p.GetPath()} scale={p.Scale}")}");

        int i = 0;
        foreach (Node ch in grid.GetChildren())
        {
            if (ch is NMerchantCard mc)
                Log($"{reason} DumpGrid slot[{i}] {SlotOneLine("cell", mc)}");
            else
                Log($"{reason} DumpGrid slot[{i}] non-NMerchantCard type={ch.GetType().Name} path={ch.GetPath()}");
            i++;
        }
    }

    public static void DumpEntireGridFromSlot(string reason, NMerchantSlot slot)
    {
        GridContainer? g = FindYgoBuyGrid(slot);
        DumpEntireGrid(reason, g);
    }

    private static NCard? TryFindMainOfferNCard(NMerchantSlot slot)
    {
        if (slot is not NMerchantCard)
            return null;
        Control? holder = slot.GetNodeOrNull<Control>("%CardHolder");
        if (holder == null)
            return null;
        foreach (Node cn in holder.GetChildren())
        {
            if (cn.Name == YgoMerchantShopBundleVisual.BundleStackNodeName)
                continue;
            if (cn is NCard nc)
                return nc;
        }

        return null;
    }

    /// <summary>Compact single-line summary for high-frequency calls (optional).</summary>
    public static string GridSummary(GridContainer? grid)
    {
        if (grid == null || !GodotObject.IsInstanceValid(grid))
            return "grid=null";

        var sb = new StringBuilder();
        sb.Append($"gridScale={grid.Scale} cells=");
        int i = 0;
        foreach (Node ch in grid.GetChildren())
        {
            if (ch is NMerchantCard mc)
            {
                float sx = YgoBuyGridMerchantSlotChromeScale.GetScaleRoot(mc)?.Scale.X
                    ?? mc.GetNodeOrNull<Control>("%CardHolder")?.Scale.X
                    ?? mc.Scale.X;
                sb.Append($"[{i}]={sx:F3} ");
            }

            i++;
        }

        return sb.ToString();
    }
}
