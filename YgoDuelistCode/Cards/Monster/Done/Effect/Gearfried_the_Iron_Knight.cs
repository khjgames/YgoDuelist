using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Destroys Equip Spells on attach, gains Energy and Gearfried Power stacks (see YgoGearfriedEquipReaction).</summary>
public sealed class Gearfried_the_Iron_Knight : EffectMonsterCard
{
    public Gearfried_the_Iron_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 18,
            baseDef: 16,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Gearfried_the_Iron_Knight) };

    public override bool IgnoresEquipSpellRaceRestrictions => true;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
