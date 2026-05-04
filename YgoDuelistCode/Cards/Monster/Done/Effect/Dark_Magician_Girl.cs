using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Gains printed <c>Mgc</c> ATK for each other Dark Magician archetype monster you control (face-up) or in your Graveyard.</summary>
public sealed class Dark_Magician_Girl : EffectMonsterCard
{
    public Dark_Magician_Girl()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 17,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.DarkMagician;

    public override Type[] RelatedCards => new[] { typeof(Dark_Magician_Girl), typeof(Dark_Magician) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int n = 0;
        foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner))
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, this))
                continue;
            if (CountsAsDarkMagicianArchetypeOtherThanGirl(m))
                n++;
        }

        foreach (CardModel c in YgoPlayerPiles.GraveyardCards(Owner))
        {
            if (c is BaseMonsterCard bm && CountsAsDarkMagicianArchetypeOtherThanGirl(bm))
                n++;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (n * mgc, 0);
    }

    private static bool CountsAsDarkMagicianArchetypeOtherThanGirl(BaseMonsterCard m) =>
        YgoMonsterArchetypeKeywords.HasKeyword(m, YgoMonsterArchetypeKeywords.DarkMagicianArchetypeKeyword)
        && m is not Dark_Magician_Girl;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
