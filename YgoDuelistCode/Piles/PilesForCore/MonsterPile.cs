using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// YgoDuelist Monster pile. Logical zone for monster cards (separate from Field if needed).
/// Hidden like Draw/Discard; summoned creatures handle their own visuals.
/// </summary>
public sealed class MonsterPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public MonsterPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
