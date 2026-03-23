using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// Combat Extra Deck: clones of fusion monsters from the run Extra Deck for the current fight.
/// </summary>
public sealed class ExtraDeckPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public ExtraDeckPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
