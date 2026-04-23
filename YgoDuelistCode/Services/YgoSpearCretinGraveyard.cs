using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Spear_Cretin"/>: sent to the Graveyard the turn it was flipped — Special Summon 1 monster from the Graveyard.</summary>
public static class YgoSpearCretinGraveyard
{
    private static readonly LocString GyPrompt =
        new("cards", "YGODUELIST-SPEAR_CRETIN.gy_summon_select");

    private static readonly LocString PositionPrompt =
        new("cards", "YGODUELIST-SPEAR_CRETIN.summon_heads_or_face_down");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Spear_Cretin sc)
            return;
        if (!sc.FlippedThisTurn)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, sc));
    }

    private static List<BaseMonsterCard> BuildGraveyardSummons(Player player, Spear_Cretin sourceInGy)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => !ReferenceEquals(m, sourceInGy) && m.CanSummonDuelMonster)
            .ToList();
    }

    private static async Task RunAsync(Player player, Spear_Cretin sourceInGy)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = BuildGraveyardSummons(player, sourceInGy);
        if (candidates.Count == 0)
            return;

        var ctx = YgoChoiceContexts.Blocking();

        BaseMonsterCard? summon = candidates.Count == 1
            ? candidates[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(GyPrompt, 1, 1) { Cancelable = true },
                () => BuildGraveyardSummons(player, sourceInGy));
        if (summon == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, summon))
            return;

        CombatState? cs = player.Creature?.CombatState;
        if (cs == null)
            return;

        var heads = cs.CreateCard<Heads>(player);
        heads.InitializeSource(sourceInGy);
        var tails = cs.CreateCard<Tails>(player);
        tails.InitializeSource(sourceInGy);

        List<CardModel> BuildPositionOptions() => new List<CardModel> { heads, tails };

        CardModel? posPick = await YgoOrderedCardSelection.TryChooseSingleAsync<CardModel>(
            ctx,
            player,
            new CardSelectorPrefs(PositionPrompt, 1, 1) { Cancelable = true },
            BuildPositionOptions);
        if (posPick == null)
            return;

        bool faceDownDefense = posPick is Tails;
        if (faceDownDefense)
            summon.FaceDown = true;

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, ctx))
            return;
    }
}
