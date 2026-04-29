using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Xz_Tank_Cannon : FusionMonsterCard
{
    public static readonly Type[] RequiredMaterialTypes =
    {
        typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.X_Head_Cannon),
        typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Z_Metal_Tank)
    };

    public Xz_Tank_Cannon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 24,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.X_Head_Cannon),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Z_Metal_Tank))
    {
    }
    
    public override YgoCardPackTags PackTags => YgoCardPackTags.None;

    public override bool CanBeFusionSummoned => false;

    public static bool PlayerHasInExtraDeck(Player player) =>
        UnionFusionSpecialSummonRules.PlayerHasFusionInExtraDeck<Xz_Tank_Cannon>(player);

    public static bool TryGetExactFieldMaterials(Player player, out List<BaseMonsterCard> materials) =>
        UnionFusionSpecialSummonRules.TryGetExactFieldMaterials(player, RequiredMaterialTypes, out materials);

}
