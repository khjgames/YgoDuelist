using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Pooling;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Visual holder for a single YgoDuelist option card in the "second hand".
/// Inherits from <see cref="NHandCardHolder"/> so hover / selection /
/// targeting behaviour exactly matches the real hand, while we only
/// customize layout / positioning for the second row.
/// </summary>
public partial class NYgoOptionCardHolder : NHandCardHolder, IPoolable
{
    private static readonly MegaCrit.Sts2.Core.Logging.Logger Logger =
        new("YgoOptionHolder", MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private const string HolderName = "YGO_OPTION_CARD";

    /// <summary>Angle in degrees when not hovered (second-hand row). Used by hover tween to restore rotation.</summary>
    public float OptionRestAngleDegrees { get; set; }

    /// <summary>When set, drag during targeting is clamped to within ~100px of this position (parent-local, set in CenterCard).</summary>
    public Vector2? OptionDragAnchor { get; set; }

    // Simple pool for holders; we expect at most 5.
    public static NYgoOptionCardHolder NewInstanceForPool()
    {
        var holder = new NYgoOptionCardHolder
        {
            Name = HolderName,
            // Match base hand_card_holder: root ignores mouse so children (hitbox) receive it.
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        // Base hand uses NCardHolderHitbox (NButton) which calls ConnectSignals() in _Ready
        // so MouseEntered/MouseExited fire. NClickableControl never calls ConnectSignals(),
        // so use NButton so the hitbox actually receives hover.
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

        // NHandCardHolder._Ready expects a "Flash" control and a "%HandIndex"
        // label in the scene. Provide minimal stand-ins so base logic and
        // highlighting work without null refs.
        var flash = new Control
        {
            Name = "Flash"
        };
        holder.AddChild(flash);
        flash.Owner = holder;

        var handIndexLabel = new MegaCrit.Sts2.addons.mega_text.MegaLabel
        {
            // For GetNode("%HandIndex") the unique-name target must be "HandIndex".
            Name = "HandIndex",
            UniqueNameInOwner = true,
            Visible = false
        };
        holder.AddChild(handIndexLabel);
        handIndexLabel.Owner = holder;

        return holder;
    }

    public static NYgoOptionCardHolder Create()
    {
        return NodePool.Get<NYgoOptionCardHolder>();
    }

    public NYgoOptionCardHolder Initialize(CardModel model, int index, int count, Vector2 viewportSize)
    {
        Visible = true;
        // Create a fresh NCard bound to this model. NCard.Create sets Model
        // and handles visibility; UpdateVisuals with Hand semantics ensures
        // correct cost colors / preview text like a normal hand card.
        NCard? cardNode = NCard.Create(model);
        if (cardNode == null)
            return this;

        cardNode.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);

        if (!IsAncestorOf(cardNode))
        {
            AddChild(cardNode);
            cardNode.Owner = this;
        }

        SetCard(cardNode);

        // Card is added last so it draws on top; make it ignore mouse so the hitbox behind receives hover/click.
        if (CardNode != null)
            CardNode.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Do NOT set position/scale/angle here – NPlayerHand.RefreshLayout
        // will call SetDefaultTargets, and our Harmony patch on that method
        // will lay out these holders in their own second row.
        return this;
    }

    public static Vector2 ComputeRowPositionForPatch(Vector2 viewportSize, int index, int count)
    {
        const float rowYFromBottom = 680f;
        const float horizontalSpacing = 155f;

        float centerX = viewportSize.X * 0.5f;
        float baseY = viewportSize.Y - rowYFromBottom;

        float totalWidth = (count - 1) * horizontalSpacing;
        float startX = centerX - totalWidth * 0.43f;

        float x = startX + horizontalSpacing * index;
        float y = baseY;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Override to avoid base CreateHoverTips calling SetAlignmentForCardHolder → SetFollowOwner(),
    /// which throws NullReferenceException for option holders (hover tip owner/parent not set up like hand cards).
    /// We only create and show the tip with default alignment; no follow-owner.
    /// </summary>
    protected override void CreateHoverTips()
    {
        if (CardNode == null)
            return;

        NHoverTipSet.CreateAndShow(this, CardNode.Model.HoverTips);
    }

    public void Reset()
    {
        if (CardNode != null && IsAncestorOf(CardNode))
        {
            CardNode.QueueFreeSafely();
        }
        Clear();
    }

    void IPoolable.OnInstantiated()
    {
    }

    void IPoolable.OnReturnedFromPool()
    {
        Visible = true;
        Scale = Vector2.One * 0.8f;
        if (Hitbox != null)
        {
            Hitbox.Visible = true;
            Hitbox.SetEnabled(true);
        }
    }

    void IPoolable.OnFreedToPool()
    {
        Reset();
    }
}
