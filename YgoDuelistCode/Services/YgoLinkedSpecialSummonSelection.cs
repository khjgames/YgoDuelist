using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoLinkedSpecialSummonSelection
{
    public static List<BaseMonsterCard> BuildMonsterCandidates(
        Player? player,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<BaseMonsterCard, bool>? predicate = null)
    {
        return YgoPileSearchSelection
            .BuildCandidates(
                player,
                sourcePiles,
                c => c is BaseMonsterCard bm
                    && (predicate == null || predicate(bm))
                    && ReactorSlimeSummonGate.AllowsSummon(player, bm))
            .OfType<BaseMonsterCard>()
            .ToList();
    }

    public static bool CanPlay(
        Player? player,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<BaseMonsterCard, bool>? predicate = null)
    {
        return player != null
            && BuildMonsterCandidates(player, sourcePiles, predicate).Count > 0
            && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, tributeReleaseCount: 0);
    }

    public static async Task<bool> TryPrepareSingleMonsterAsync(
        Player player,
        CardModel sourceCard,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        LocString prompt,
        Func<BaseMonsterCard, bool>? predicate = null)
    {
        List<BaseMonsterCard> candidates = BuildMonsterCandidates(player, sourcePiles, predicate);
        if (candidates.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<BaseMonsterCard>(
            player,
            sourceCard,
            candidates,
            YgoCancelableConfirmGridPrefs.ForSinglePick(prompt),
            rebuildCanonicalForRemoteApply: () => BuildMonsterCandidates(player, sourcePiles, predicate)
                .Cast<CardModel>()
                .ToList());
    }

    public static async Task<BaseMonsterCard?> TryResolvePreparedSpecialSummonAsync(
        PlayerChoiceContext choiceContext,
        Player? player,
        CardModel sourceCard,
        IReadOnlyList<YgoSearchPile> sourcePiles,
        Func<BaseMonsterCard, bool>? predicate = null)
    {
        if (player == null)
            return null;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(sourceCard, out CardModel? picked)
            || picked is not BaseMonsterCard chosen)
            return null;

        List<BaseMonsterCard> legalNow = BuildMonsterCandidates(player, sourcePiles, predicate);
        if (!legalNow.Contains(chosen))
            return null;

        bool summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext);
        return summoned ? chosen : null;
    }

    public static void AttachLinkedTrapIfPending<TTrap>(
        TTrap trap,
        ref BaseMonsterCard? pendingLinkAfterZone)
        where TTrap : CardModel, IYgoSpellTrapEquipLink
    {
        if (pendingLinkAfterZone == null)
            return;

        YgoSpellTrapEquipLinkRegistry.Attach(trap, pendingLinkAfterZone);
        pendingLinkAfterZone = null;
    }
}