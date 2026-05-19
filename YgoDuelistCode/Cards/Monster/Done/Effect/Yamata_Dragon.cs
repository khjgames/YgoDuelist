using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. When this card deals unblocked damage, draw until you have 5 cards in your hand.
/// </summary>
public sealed class Yamata_Dragon : SpiritEffectMonsterCard
{
    private const int TargetHandSize = 5;

    public override int AttackPortionCount => 5;

    public Yamata_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 26,
            baseDef: 31,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            duelMonsterAttackPlayEnergyOverride: 0,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Burn | YgoCardPackTags.Dragon | YgoCardPackTags.Fire;

    public override Type[] RelatedCards => new[] { typeof(Flame_Ruler) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    public override async Task OnFirstUnblockedDamageToEnemyThisChainAsync(
        AttackCommand command,
        DamageResult r,
        Player atkPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        _ = command;
        _ = r;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(atkPlayer, this))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(atkPlayer);
        CardPile? draw = YgoPlayerPiles.Draw(atkPlayer);
        if (hand == null || draw == null)
            return;

        while (hand.Cards.Count < TargetHandSize && draw.Cards.Count > 0)
            await CardPileCmd.Draw(ctx, 1, atkPlayer);
    }
}
