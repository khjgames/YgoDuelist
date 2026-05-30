using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NPlayerHand), "RefreshLayout")]
public static class YgoSecondHandLayoutPatch
{
    static bool Prefix(NPlayerHand __instance)
    {
        // Split holders into main and option rows.
        var all = __instance.ActiveHolders.ToList();
        var main = all.Where(h => h is not NYgoOptionCardHolder).ToList();
        var opts = all.Where(h => h is NYgoOptionCardHolder).ToList();
        GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch ENTER all=", all.Count, " main=", main.Count, " opts=", opts.Count, " FocusedHolderId=", __instance.FocusedHolder?.GetInstanceId() ?? 0, " FocusedIsOpt=", __instance.FocusedHolder is NYgoOptionCardHolder);
        for (int o = 0; o < opts.Count; o++)
            GD.Print("[YgoDuelist]   opts[", o, "] holderId=", opts[o].GetInstanceId(), " model=", (opts[o] as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?");

        // ----- Main hand: vanilla layout logic, but over 'main' only -----
        int count = main.Count;
        if (count > 0)
        {
            int handSize = count;
            Vector2 scale = HandPosHelper.GetScale(count);

            // Focused index within main-only list.
            int focusedIdx = -1;
            var focused = __instance.FocusedHolder;
            if (focused != null)
            {
                focusedIdx = main.IndexOf(focused);
            }

            // Access private fields needed for disable offset and dragged index/labels.
            var isDisabledField = AccessTools.Field(typeof(NPlayerHand), "_isDisabled");
            bool isDisabled = isDisabledField?.GetValue(__instance) is true;

            var disablePosField = AccessTools.Field(typeof(NPlayerHand), "_disablePosition");
            Vector2 disablePos = disablePosField?.GetValue(null) is Vector2 disableVector
                ? disableVector
                : Vector2.Zero;

            var draggedIndexField = AccessTools.Field(typeof(NPlayerHand), "_draggedHolderIndex");
            int draggedIndex = draggedIndexField?.GetValue(__instance) is int indexValue ? indexValue : -1;
            bool hasDraggedHolder = draggedIndex >= 0;

            for (int i = 0; i < count; i++)
            {
                int cardIndex = i;
                Vector2 position = HandPosHelper.GetPosition(handSize, cardIndex);
                if (focusedIdx > -1)
                {
                    float num2 = Mathf.Lerp(100f, 0f, Mathf.Min(1f, (float)Mathf.Abs(focusedIdx - i) / 4f));
                    position += Vector2.Left * Mathf.Sign(focusedIdx - i) * num2;
                }

                NHandCardHolder holder = main[i];
                if (focusedIdx == i)
                {
                    holder.SetAngleInstantly(0f);
                    holder.SetScaleInstantly(Vector2.One);
                    float hitH = holder.Hitbox?.Size.Y ?? holder.Size.Y;
                    position.Y = (0f - hitH) * 0.5f + 2f;
                    if (isDisabled)
                    {
                        position -= disablePos;
                    }
                    holder.Position = new Vector2(holder.Position.X, position.Y);
                    holder.SetTargetPosition(position);
                }
                else
                {
                    holder.SetTargetPosition(position);
                    holder.SetTargetScale(scale);
                    holder.SetTargetAngle(HandPosHelper.GetAngle(handSize, cardIndex));
                }

                if (holder.Hitbox != null)
                    holder.Hitbox.MouseFilter = hasDraggedHolder
                        ? Control.MouseFilterEnum.Ignore
                        : Control.MouseFilterEnum.Stop;

                // Focus neighbors (left/right) – still based on main-only list.
                NodePath leftPath;
                if (i <= 0)
                {
                    leftPath = main[main.Count - 1].GetPath();
                }
                else
                {
                    leftPath = main[i - 1].GetPath();
                }
                holder.FocusNeighborLeft = leftPath;
                holder.FocusNeighborRight = (i < main.Count - 1 ? main[i + 1].GetPath() : main[0].GetPath());
                holder.FocusNeighborBottom = holder.GetPath();

                // Index labels match vanilla semantics within the main row.
                if (hasDraggedHolder && i >= draggedIndex)
                {
                    holder.SetIndexLabel(i + 2);
                }
                else
                {
                    holder.SetIndexLabel(i + 1);
                }
            }
        }

        // ----- Option hand: independent second row for NYgoOptionCardHolder -----
        int optCount = opts.Count;
        if (optCount > 0)
        {
            int handSize = optCount;

            // Second hand uses a fixed smaller scale compared to the normal hand.
            Vector2 smallScale = Vector2.One * 0.45f;

            // Focused index within option-only list, for hover zoom behavior.
            int optFocusedIdx = -1;
            var focusedOpt = __instance.FocusedHolder;
            if (focusedOpt is NYgoOptionCardHolder)
            {
                optFocusedIdx = opts.IndexOf(focusedOpt);
            }

            for (int i = 0; i < optCount; i++)
            {
                int cardIndex = i;
                Vector2 position = HandPosHelper.GetPosition(handSize, cardIndex) * 0.9f;

                // Slightly tighten the horizontal spread.
                position.X *= 0.85f;

                // Place the option row above the main hand row.
                position.Y = -745f;

                NHandCardHolder holder = opts[i];
                float restAngle = HandPosHelper.GetAngle(handSize, cardIndex) * 0.85f;
                if (holder is NYgoOptionCardHolder optHolder)
                {
                    // Clear drag anchor so SetTargetPosition(optionRowPosition) is not overwritten by
                    // NHandCardHolderSetTargetPositionOptionHolderPatch (which clamps to mouse during targeting).
                    optHolder.OptionDragAnchor = null;
                    optHolder.OptionRestAngleDegrees = restAngle;
                }
                holder.SetTargetPosition(position);
                holder.Visible = true;
                if (holder.Hitbox != null)
                {
                    holder.Hitbox.Visible = true;
                    holder.Hitbox.SetEnabled(true);
                    // Option row must always accept input after a play: vanilla drag paths can leave
                    // MouseFilter.Ignore on holders; we never reset it here before, so siblings (e.g. field
                    // spells) could stay unclickable after another option card was dragged.
                    holder.Hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
                }
                holder.ZIndex = 0;

                GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.Position.Y = ", holder.Position.Y);
                GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.TargetPosition.Y = ", holder.TargetPosition.Y);
                if (holder.Hitbox != null)
                    GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.Hitbox.Position.Y = ", holder.Hitbox.Position.Y);

                if (optFocusedIdx == i)
                {
                    // Hover zoom: animate the focused option card toward full size,
                    // like a normal hand card, so it's readable.
                    holder.SetTargetScale(Vector2.One);
                    holder.SetTargetAngle(restAngle);
                }
                else
                {
                    holder.SetTargetScale(smallScale);
                    holder.SetTargetAngle(restAngle);
                }
            }
            // Deferred validation one frame after layout to catch "failed to wire" state (e.g. can't hover).
            var combatStateField = AccessTools.Field(typeof(NPlayerHand), "_combatState");
            var state = combatStateField?.GetValue(__instance) as CombatState;
            if (state != null)
            {
                var player = LocalContext.GetMe(state);
                if (player != null)
                {
                    var tree = __instance.GetTree();
                    if (tree != null)
                    {
                        var timer = tree.CreateTimer(0.0);
                        timer.Timeout += () => YgoOptionHandUiPatch.ValidateSecondHandHolders(player);
                    }
                }
            }
        }

        GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch EXIT return false");
        return false;
    }
}
