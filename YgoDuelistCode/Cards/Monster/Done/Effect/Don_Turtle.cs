using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Don_Turtle : EffectMonsterCard
{
    public Don_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 11,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Draw;
    public override Type[] RelatedCards => new[] { typeof(Don_Turtle) };

    public override bool BundleGrantsExtraCopyOfSelf => true;

    public override Type[] BundledCards => new[] { typeof(Don_Turtle) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async () =>
            {
                if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                    return;
                CardPile? hand = YgoPlayerPiles.Hand(player);
                if (hand == null)
                    return;
                List<Don_Turtle> copies = YgoMpCombatOrder
                    .CardsSnapshotOrderedForMp(hand.Cards)
                    .OfType<Don_Turtle>()
                    .Where(ReactorSlimeSummonGate.SummonCandidatePredicate<Don_Turtle>(player))
                    .ToList();
                foreach (Don_Turtle copy in copies)
                {
                    if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                        break;
                    await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, copy, choiceContext);
                }
            });
}
