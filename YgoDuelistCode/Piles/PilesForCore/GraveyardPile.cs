using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// YgoDuelist Graveyard pile. Hidden on the table like Draw/Discard; cards are not shown as NCards.
/// Registered via BaseLib [CustomEnum] and used as the combat destination for discarded/destroyed cards.
/// </summary>
public sealed class GraveyardPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public GraveyardPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
