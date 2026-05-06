using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Marauding_Captain : EffectMonsterCard
{
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-MARAUDING_CAPTAIN.summon_from_hand");

    public Marauding_Captain()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 4,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Marauding_Captain) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async ctx =>
            {
                if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                    return;

                List<BaseMonsterCard> candidates = BuildHandSummonCandidates(player);
                if (candidates.Count == 0)
                    return;

                BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                    ctx,
                    player,
                    new CardSelectorPrefs(SummonPrompt, 0, 1)
                    {
                        RequireManualConfirmation = true,
                        Cancelable = true
                    },
                    () => BuildHandSummonCandidates(player));
                if (chosen == null)
                    return;
                if (chosen.Pile?.Type != PileType.Hand)
                    return;

                await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
            });
    }

    private static List<BaseMonsterCard> BuildHandSummonCandidates(Player player)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m.DuelMonsterLevel <= 4
                && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
            .Where(ReactorSlimeSummonGate.SummonCandidatePredicate<BaseMonsterCard>(player))
            .ToList();
    }
}
