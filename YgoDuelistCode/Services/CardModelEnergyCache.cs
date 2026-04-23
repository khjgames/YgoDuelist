using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="CardModel.EnergyCost"/> caches the first resolved canonical cost; clear when canonical changes (e.g. monster stance,
/// <see cref="MonsterCommandCard.InitializeSource"/>, forge preview, or after <see cref="CardModel.FinalizeUpgradeInternal"/> for
/// <see cref="AbstractMonsterCard"/> after smithing — see <c>CardModelFinalizeUpgradeEnergyCachePatch</c>).
/// </summary>
public static class CardModelEnergyCache
{
    private static readonly FieldInfo? EnergyCostBacking = typeof(CardModel).GetField(
        "_energyCost",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Invalidate(CardModel card)
    {
        if (card == null)
            return;
        // Library / compendium uses immutable canonical instances; attack↔defense toggle still changes
        // <see cref="AbstractMonsterCard.CanonicalEnergyCost"/> and must drop the cached <see cref="CardModel.EnergyCost"/>.
        if (!card.IsMutable && card is not AbstractMonsterCard && card is not MonsterCommandCard)
            return;
        EnergyCostBacking?.SetValue(card, null);
        card.InvokeEnergyCostChanged();
    }

    /// <summary>Clears cached play energy for monsters in hand (e.g. Cost Down).</summary>
    public static void InvalidateHandMonstersEnergy(Player? player)
    {
        if (player == null)
            return;
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;
        foreach (CardModel c in hand.Cards)
        {
            if (c is BaseMonsterCard m)
                Invalidate(m);
        }
    }
}
