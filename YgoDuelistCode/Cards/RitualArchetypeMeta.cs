using System;
using System.Linq;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Bundles and related-card weights for ritual spells and ritual monsters. Keep
/// <see cref="NonRitualCardsReferencingRitualInLocalization"/> in sync with <c>cards.json</c>:
/// non-<see cref="Core.RitualSpellCard"/> / non-<see cref="Core.RitualMonsterCard"/> entries whose text mentions ritual.
/// </summary>
public static class RitualArchetypeMeta
{
    public static readonly Type[] NonRitualCardsReferencingRitualInLocalization =
    {
        typeof(Senju_of_the_Thousand_Hands),
        typeof(Sonic_Bird),
    };

    public static Type[] RelatedCardsForPairedRitual(Type ritualSpell, Type ritualMonster)
    {
        return new[] { ritualSpell, ritualMonster }.Concat(NonRitualCardsReferencingRitualInLocalization).ToArray();
    }
}
