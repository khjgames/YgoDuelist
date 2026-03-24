using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// Banished / removed-from-play zone. Not discard, not exhaust, not the YGO Graveyard pile.
/// </summary>
public sealed class ShadowRealmPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public ShadowRealmPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
