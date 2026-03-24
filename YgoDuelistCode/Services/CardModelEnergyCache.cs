using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="CardModel.EnergyCost"/> caches the first resolved canonical cost; clear when canonical changes (e.g. monster stance).
/// </summary>
public static class CardModelEnergyCache
{
    private static readonly FieldInfo? EnergyCostBacking = typeof(CardModel).GetField(
        "_energyCost",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Invalidate(CardModel card)
    {
        if (card == null || !card.IsMutable)
            return;
        EnergyCostBacking?.SetValue(card, null);
        card.InvokeEnergyCostChanged();
    }
}
