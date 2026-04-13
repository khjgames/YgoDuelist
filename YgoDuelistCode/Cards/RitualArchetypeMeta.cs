using System;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

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

    /// <summary>
    /// One-to-one ritual spell ↔ named ritual monster pairs (excludes generic spells like Contract with the Abyss).
    /// </summary>
    private static readonly (Type Spell, Type Monster)[] PairedRitualArchetypes =
    {
        (typeof(Hamburger_Recipe), typeof(Hungry_Burger)),
        (typeof(Black_Illusion_Ritual), typeof(Relinquished)),
        (typeof(Black_Luster_Ritual), typeof(Black_Luster_Soldier)),
        (typeof(Black_Magic_Ritual), typeof(Magician_of_Black_Chaos)),
        (typeof(Beastly_Mirror_Ritual), typeof(Fiend_S_Mirror)),
        (typeof(Commencement_Dance), typeof(Performance_of_Sword)),
        (typeof(Contract_with_the_Dark_Master), typeof(Dark_Master_Zorc)),
        (typeof(Curse_of_the_Masked_Beast), typeof(The_Masked_Beast)),
        (typeof(Fortress_Whale_S_Oath), typeof(Fortress_Whale)),
        (typeof(Garma_Sword_Oath), typeof(Garma_Sword)),
        (typeof(Incandescent_Ordeal), typeof(Legendary_Flame_Lord)),
        (typeof(Javelin_Beetle_Pact), typeof(Javelin_Beetle)),
        (typeof(Novox_S_Prayer), typeof(Skull_Guardian)),
        (typeof(Resurrection_of_Chakra), typeof(Chakra)),
        (typeof(Revival_of_Dokurorider), typeof(Dokurorider)),
        (typeof(Shinato_S_Ark), typeof(Shinato_King_of_a_Higher_Plane)),
        (typeof(Turtle_Oath), typeof(Crab_Turtle)),
        (typeof(War_Lion_Ritual), typeof(Super_War_Lion)),
        (typeof(White_Dragon_Ritual), typeof(Paladin_of_White_Dragon)),
        (typeof(Zera_Ritual), typeof(Zera_the_Mant)),
    };

    /// <summary>
    /// Ritual spell type that summons this specific ritual monster, if any.
    /// </summary>
    public static Type? PairedRitualSpellType(Type ritualMonsterCardType)
    {
        foreach (var (spell, monster) in PairedRitualArchetypes)
        {
            if (monster == ritualMonsterCardType)
                return spell;
        }

        return null;
    }
}
