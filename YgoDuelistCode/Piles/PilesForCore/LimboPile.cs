using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Piles;

/// <summary>
/// Combat-only zone for Union-Effect materials and unequipped union equip spells (not graveyard / banished).
/// </summary>
public sealed class LimboPile : CustomPile
{
    [CustomEnum]
    public static PileType CustomType;

    public LimboPile() : base(CustomType)
    {
    }

    public override bool CardShouldBeVisible(CardModel card) => false;

    public override Vector2 GetTargetPosition(CardModel model, Vector2 size) => Vector2.Zero;
}
