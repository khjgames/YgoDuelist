using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// YgoDuelist Card Option pile. Logical zone for temporary option cards (e.g., generated choices).
/// Hidden like Draw/Discard by default; UI flows (card selectors, previews) control what's visible.
/// </summary>
public class YgoCardOptionPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public YgoCardOptionPile() : base(CustomType)
    {
        GD.Print($"[YgoDuelist] YgoCardOptionPile ctor: CustomType={CustomType}");
    }

    public override bool CardShouldBeVisible(CardModel card) => false;
    // For our ygo option pile second hand implementation we CANNOT AND WILL NOT USE CardShouldBeVisible
    // WE CANNOT AND WILL NOT USE GetTargetPosition or CustomTween
    // WE HAVE TO HANDLE NCARD LIFECYCLE FOR THE TEMPORARY CARDS IN THIS ZONE OURSELVES
    // MEANING VISIBILITY, CREATION, DESTRUCTION, NCARDHOLDERS, WIRING INTO BASE GAME.
    // WE MUST HANDLE IT OURSELVES AND CANNOT USE CardShouldBeVisible true, GetTargetPosition or CustomTween
    // BECAUSE WE HAVE VERY SPECIFIC SYSTEM REQUIREMENTS AND THEY ARE NOT COMPATIBLE

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
