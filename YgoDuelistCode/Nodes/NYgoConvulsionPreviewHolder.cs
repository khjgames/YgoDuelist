using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.addons.mega_text;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// A small, non-clickable card holder used for "always visible" previews
/// (e.g. Convulsion Of Nature showing the top of the draw pile).
/// Uses the same hover/zoom plumbing as hand/second-hand holders.
/// </summary>
public partial class NYgoConvulsionPreviewHolder : NHandCardHolder, IPoolable
{
    private const string HolderName = "YGO_CONVULSION_PREVIEW_CARD";

    /// <summary>Invoked from <c>_Process</c> when Convulsion preview should refresh (NCombatUi has no declared <c>_Process</c> to patch).</summary>
    public Action? ConvulsionSyncTick;

    public static NYgoConvulsionPreviewHolder NewInstanceForPool()
    {
        var holder = new NYgoConvulsionPreviewHolder
        {
            Name = HolderName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        // Minimal structure expected by NHandCardHolder._Ready (Hitbox, Flash, HandIndex).
        var hitbox = new NButton
        {
            Name = "Hitbox",
            UniqueNameInOwner = true,
            Size = new Vector2(186, 264),
            Position = new Vector2(-93, -132),
            PivotOffset = new Vector2(93, 132)
        };
        holder.AddChild(hitbox);
        hitbox.Owner = holder;

        var flash = new Control { Name = "Flash" };
        holder.AddChild(flash);
        flash.Owner = holder;

        var handIndexLabel = new MegaLabel
        {
            Name = "HandIndex",
            UniqueNameInOwner = true,
            Visible = false
        };
        // MegaLabel._Ready requires an explicit font override (inherited theme is not enough).
        handIndexLabel.AddThemeFontOverride(ThemeConstants.Label.Font, new SystemFont());
        holder.AddChild(handIndexLabel);
        handIndexLabel.Owner = holder;

        return holder;
    }

    public static NYgoConvulsionPreviewHolder Create()
    {
        // This node is not backed by a .tscn scene, so we construct it directly.
        // (Avoids requiring NodePool.Init for a scene path.)
        return NewInstanceForPool();
    }

    public void EnsureCard(CardModel model)
    {
        if (CardNode == null)
        {
            NCard? cardNode = NCard.Create(model);
            if (cardNode == null)
                return;

            cardNode.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
            this.AddChildSafely(cardNode);
            cardNode.Owner = this;
            SetCard(cardNode);

            // Make sure the hitbox receives hover/click.
            CardNode.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        else
        {
            ReassignToCard(model, PileType.Hand, target: null, ModelVisibility.Visible);
        }
    }

    public override void _Process(double delta)
    {
        ConvulsionSyncTick?.Invoke();
        base._Process(delta);
    }

    /// <summary>
    /// Prevents "play card" interactions; this holder is display-only.
    /// </summary>
    protected override void OnMousePressed(InputEvent inputEvent)
    {
    }

    /// <summary>
    /// Avoid base alignment logic that assumes full hand context; just show default hover tips.
    /// </summary>
    protected override void CreateHoverTips()
    {
        if (CardNode == null)
            return;

        NHoverTipSet.CreateAndShow(this, CardNode.Model.HoverTips);
    }

    void IPoolable.OnInstantiated()
    {
    }

    void IPoolable.OnReturnedFromPool()
    {
        ConvulsionSyncTick = null;
        SetProcess(false);
        Visible = true;
        if (Hitbox != null)
        {
            Hitbox.Visible = true;
            Hitbox.SetEnabled(true);
        }
    }

    void IPoolable.OnFreedToPool()
    {
        ConvulsionSyncTick = null;
        SetProcess(false);
        if (CardNode != null && IsAncestorOf(CardNode))
            CardNode.QueueFreeSafely();

        Clear();
    }
}

