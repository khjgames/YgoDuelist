using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Vwxyz_Dragon_Catapult_Cannon : FusionMonsterCard
{
    public static readonly Type[] RequiredMaterialTypes =
    {
        typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion.Vw_Tiger_Catapult),
        typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion.Xyz_Dragon_Cannon)
    };

    public override int AttackPortionCount => 3;

    public Vwxyz_Dragon_Catapult_Cannon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 30,
            baseDef: 28,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion.Vw_Tiger_Catapult),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion.Xyz_Dragon_Cannon))
    {
    }
    
    public override YgoCardPackTags PackTags => YgoCardPackTags.None;

    public override bool CanBeFusionSummoned => false;

    public static bool PlayerHasInExtraDeck(Player player) =>
        UnionFusionSpecialSummonRules.PlayerHasFusionInExtraDeck<Vwxyz_Dragon_Catapult_Cannon>(player);

    public static bool TryGetExactFieldMaterials(Player player, out List<BaseMonsterCard> materials) =>
        UnionFusionSpecialSummonRules.TryGetExactFieldMaterials(player, RequiredMaterialTypes, out materials);

}
