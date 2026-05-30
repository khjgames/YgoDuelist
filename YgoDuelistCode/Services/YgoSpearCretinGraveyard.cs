using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Spear_Cretin"/>: sent to the Graveyard the turn it was flipped — Special Summon 1 monster from the Graveyard.</summary>
public static class YgoSpearCretinGraveyard
{
    private static readonly LocString GyPrompt =
        new("cards", "YGODUELIST-SPEAR_CRETIN.gy_summon_select");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Spear_Cretin sc)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        if (!sc.YgoDuelist_FlippedThisTurn)
        {
            GD.Print(
                $"[YgoDuelist][MP][SpearCretin] skip GY effect (YgoDuelist_FlippedThisTurn=false) ownerNet={sc.Owner?.NetId} id={sc.Id?.Entry}");
            return;
        }

        Player ownerPlayer = player;
        Spear_Cretin spear = sc;
        void StartRun()
        {
            GD.Print(
                $"[YgoDuelist][MP][SpearCretin] start GY effect ownerNet={ownerPlayer.NetId} id={spear.Id?.Entry}");
            TaskHelper.RunSafely(RunAsync(ownerPlayer, spear));
        }

        if (Engine.GetMainLoop() is SceneTree tree && tree.Root != null)
            Callable.From(StartRun).CallDeferred();
        else
            StartRun();
    }

    private static List<BaseMonsterCard> BuildGraveyardSummons(Player player, Spear_Cretin sourceInGy)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m =>
                !ReferenceEquals(m, sourceInGy)
                && m.CanSummonDuelMonster
                && ReactorSlimeSummonGate.AllowsSummon(player, m))
            .ToList();
    }

    /// <summary>
    /// Face-up Attack vs face-down Defense: player right-clicks the row to cycle Attack / Defense (see NCardHolder Alt /
    /// right-click patches). Defense mode maps to face-down set summon; Attack mode to face-up Attack.
    /// </summary>
    private static void ApplySpearCretinSummonStanceFromGrid(AbstractMonsterCard summon, bool faceDownDefenseSet)
    {
        if (faceDownDefenseSet)
            summon.ApplyNetworkObserverHandPlayBattleState(false, false, faceDownValue: true, willSetValue: true);
        else
            summon.ApplyNetworkObserverHandPlayBattleState(true, false, faceDownValue: false, willSetValue: false);
    }

    private static async Task RunAsync(Player player, Spear_Cretin sourceInGy)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = BuildGraveyardSummons(player, sourceInGy);
        if (candidates.Count == 0)
            return;

        var ctx = YgoChoiceContexts.Blocking();

        BaseMonsterCard? summon = await TryChooseGraveyardSummonAsync(ctx, player, sourceInGy);
        if (summon == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, summon))
            return;

        if (player.Creature?.CombatState == null)
            return;

        bool faceDownDefenseSet = !summon.IsAttackBattlePosition && summon.FaceDown;
        ApplySpearCretinSummonStanceFromGrid(summon, faceDownDefenseSet);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, ctx))
        {
            GD.PrintErr(
                $"[YgoDuelist][SpearCretin] TrySummonDuelMonsterSpecial failed target={summon.Id?.Entry} faceDownSet={faceDownDefenseSet} atkBattle={summon.IsAttackBattlePosition} type={summon.Type} faceDown={summon.FaceDown} ownerNet={player.NetId}");
        }
    }

    private static async Task<BaseMonsterCard?> TryChooseGraveyardSummonAsync(
        PlayerChoiceContext ctx,
        Player player,
        Spear_Cretin sourceInGy)
    {
        try
        {
            YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = true;
            return await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(GyPrompt, 1, 1)
                {
                    Cancelable = true,
                    RequireManualConfirmation = true,
                },
                () => BuildGraveyardSummons(player, sourceInGy));
        }
        finally
        {
            YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = false;
        }
    }
}
