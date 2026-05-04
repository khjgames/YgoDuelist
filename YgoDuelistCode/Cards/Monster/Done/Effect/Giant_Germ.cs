using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Battle-death Blight + deck SS — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGiantGermGraveyard"/>.</summary>
public sealed class Giant_Germ : EffectMonsterCard
{
    public Giant_Germ()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 10,
            baseDef: 1,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Burn | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Giant_Germ) };

    public override bool BundleGrantsExtraCopyOfSelf => true;

    public override Type[] BundledCards => new[] { typeof(Giant_Germ) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 7m;
    }
}
