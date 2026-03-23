using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// Hidden logical pile for the in-combat Spell/Trap zone cards.
/// Visuals are rendered through the custom second-hand pipeline.
/// </summary>
public sealed class SpellTrapZonePile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public SpellTrapZonePile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
