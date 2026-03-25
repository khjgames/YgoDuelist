using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="CardModel.EnergyCost"/> caches the first resolved canonical cost; clear when canonical changes (e.g. monster stance
/// or <see cref="MonsterCommandCard.InitializeSource"/>).
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
}
