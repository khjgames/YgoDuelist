using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 3 Fiend monsters from your Graveyard to Special Summon.
/// When sent from the field to the Graveyard: you may Special Summon 1 Fiend with level at most {Mgc} from your Graveyard or Banished.
/// </summary>
public sealed class Dark_Necrofear : EffectMonsterCard
{
    private static readonly LocString BanishFiendsPrompt =
        new("cards", "YGODUELIST-DARK_NECROFEAR.banish_three_fiends_graveyard");

    private static readonly LocString SummonFiendPrompt =
        new("cards", "YGODUELIST-DARK_NECROFEAR.summon_fiend_gy_or_banished");

    public Dark_Necrofear()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 22,
            baseDef: 28,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Dark_Necrofear) };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;

            List<BaseMonsterCard> banished = await YgoOrderedCardSelection.TryChooseManyAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishFiendsPrompt, 3, 3)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildFiendGraveyardBanishCandidates(player),
                maxResults: 3);
            if (banished.Count < 3)
                return;

            foreach (BaseMonsterCard m in banished)
                await YgoBanishedService.BanishCard(player, m);

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public override Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        TaskHelper.RunSafely(TryOptionalSpecialSummonFiendFromGyOrBanishedAsync(ctx.Player));
        return base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    private async Task TryOptionalSpecialSummonFiendFromGyOrBanishedAsync(Player player)
    {
        if (player?.Creature?.CombatState == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        int maxLevel = (int)DynamicVars["Mgc"].BaseValue;
        if (maxLevel <= 0)
            return;

        List<BaseMonsterCard> candidates = BuildFiendGyOrBanishedSummonCandidates(player, maxLevel);
        if (candidates.Count == 0)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(SummonFiendPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildFiendGyOrBanishedSummonCandidates(player, maxLevel));
        if (chosen == null)
            return;

        if (!IsInPlayerGyOrBanished(player, chosen))
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildFiendGraveyardBanishCandidates(player).Count >= 3
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.Fiend);
    }

    private static List<BaseMonsterCard> BuildFiendGraveyardBanishCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Fiend)
            .ToList();

    private List<BaseMonsterCard> BuildFiendGyOrBanishedSummonCandidates(Player player, int maxLevel)
    {
        var list = new List<BaseMonsterCard>();
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy != null)
        {
            list.AddRange(
                YgoMpCombatOrder
                    .CardsSnapshotOrderedForMp(gy.Cards)
                    .OfType<BaseMonsterCard>()
                    .Where(m =>
                        m.DuelMonsterRace == DuelMonsterRace.Fiend
                        && m.DuelMonsterLevel <= maxLevel
                        && !m.BlocksSpecialDuelMonsterSummon
                        && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
                    .Where(ReactorSlimeSummonGate.SummonCandidatePredicate<BaseMonsterCard>(player)));
        }

        CardPile? ban = YgoPlayerPiles.Banished(player);
        if (ban != null)
        {
            list.AddRange(
                YgoMpCombatOrder
                    .CardsSnapshotOrderedForMp(ban.Cards)
                    .OfType<BaseMonsterCard>()
                    .Where(m =>
                        m.DuelMonsterRace == DuelMonsterRace.Fiend
                        && m.DuelMonsterLevel <= maxLevel
                        && !m.BlocksSpecialDuelMonsterSummon
                        && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
                    .Where(ReactorSlimeSummonGate.SummonCandidatePredicate<BaseMonsterCard>(player)));
        }

        return list;
    }

    private static bool IsInPlayerGyOrBanished(Player player, BaseMonsterCard card) =>
        YgoPlayerPiles.GraveyardContains(player, card)
        || YgoPlayerPiles.Banished(player)?.Cards.Contains(card) == true;
}
