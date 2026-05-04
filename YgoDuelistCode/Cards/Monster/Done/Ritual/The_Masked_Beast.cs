using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;

public sealed class The_Masked_Beast : RitualMonsterCard
{
    public The_Masked_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 32,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Ritual | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Bundled;

    /// <summary>Ritual spell bundled with the monster when sold in the YGO merchant (see <see cref="YgoDuelistCard.BundledCards"/>).</summary>
    public override Type[] BundledCards => new[] { typeof(Curse_of_the_Masked_Beast) };
}
