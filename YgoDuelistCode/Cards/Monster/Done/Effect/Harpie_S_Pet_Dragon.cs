using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Gains {Mgc} ATK/DEF for each face-up Harpie Lady on the field.</summary>
public sealed class Harpie_S_Pet_Dragon : EffectMonsterCard
{
    public Harpie_S_Pet_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 20,
            baseDef: 25,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Wind | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards => new[] { typeof(Harpie_S_Pet_Dragon), typeof(Harpie_Lady) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Harpie_Lady));

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner?.Creature?.CombatState is not { } cs)
            return base.GetSecondaryStats();

        int harpies = 0;
        foreach (var player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            foreach (BaseMonsterCard monster in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
            {
                if (monster.FaceDown || monster is not Harpie_Lady)
                    continue;
                harpies++;
            }
        }

        int bonusEach = (int)DynamicVars["Mgc"].BaseValue;
        int bonus = Math.Clamp(harpies * bonusEach, 0, 9999);
        return (bonus, bonus);
    }
}
