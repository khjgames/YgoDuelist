using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// YgoDuelist Field pile. Logical zone for cards "on the field".
/// Currently hidden like Draw/Discard (no NCards shown); monsters themselves handle visuals.
/// </summary>
public sealed class FieldPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public FieldPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
