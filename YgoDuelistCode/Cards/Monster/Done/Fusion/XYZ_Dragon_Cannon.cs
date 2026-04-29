using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Xyz_Dragon_Cannon : FusionMonsterCard
{
    public static readonly Type[] RequiredMaterialTypes =
    {
        typeof(X_Head_Cannon),
        typeof(Y_Dragon_Head),
        typeof(Z_Metal_Tank)
    };

    public override int AttackPortionCount => 3;

    public Xyz_Dragon_Cannon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 26,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(X_Head_Cannon),
            typeof(Y_Dragon_Head),
            typeof(Z_Metal_Tank))
    {
    }

    public override Type[] BundledCards => new[] { typeof(Xy_Dragon_Cannon), typeof(Xz_Tank_Cannon), typeof(Yz_Tank_Dragon) };

    public override bool CanBeFusionSummoned => false;

    public static bool PlayerHasInExtraDeck(Player player) =>
        UnionFusionSpecialSummonRules.PlayerHasFusionInExtraDeck<Xyz_Dragon_Cannon>(player);

    public static bool TryGetExactFieldMaterials(Player player, out List<BaseMonsterCard> materials) =>
        UnionFusionSpecialSummonRules.TryGetExactFieldMaterials(player, RequiredMaterialTypes, out materials);

}