using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>
/// Builds <see cref="NetYgoMonsterMenuCommandAction"/> from option-pile cards and enqueues
/// <see cref="YgoMonsterMenuCommandGameAction"/> during combat so MP peers mirror state.
/// </summary>
public static class YgoMonsterMenuCommandNetHelper
{
    /// <summary>
    /// Prefer synchronized queue when in combat; otherwise run the same effect locally (single-player / no synchronizer).
    /// </summary>
    public static void TryEnqueueOrRunLocal(CardModel card, Creature? enemyTarget)
    {
        if (card?.Owner == null)
            return;

        if (YgoNetCombatActionRouter.IsMultiplayerCombatQueueActive)
        {
            if (TryBuildPayload(card, enemyTarget, out NetYgoMonsterMenuCommandAction payload))
            {
                var gameAction = new YgoMonsterMenuCommandGameAction(card.Owner, payload);
                if (YgoNetCombatActionRouter.TryRequestEnqueue(gameAction, "YgoMonsterMenuCommand"))
                    return;

                RunLocalFallback(card, enemyTarget);
                return;
            }

            GD.PrintErr($"[YgoDuelist][MP] YgoMonsterMenuCommandNetHelper: could not build payload for {card.GetType().Name}");
        }

        RunLocalFallback(card, enemyTarget);
    }

    private static void RunLocalFallback(CardModel card, Creature? enemyTarget)
    {
        switch (card)
        {
            case Exit_Monster_Options exit:
                TaskHelper.RunSafely(exit.OnClickedOption());
                return;
            case Command_Change_Battle_Position changePos:
                TaskHelper.RunSafely(changePos.OnClickedOption(enemyTarget));
                return;
            case Toggle_Die_For_You toggle:
                TaskHelper.RunSafely(toggle.OnClickedOption());
                return;
        }
    }

    /// <summary>True when <paramref name="card"/> is in this player's YGO option pile.</summary>
    public static bool IsInYgoOptionPile(CardModel card, Player player)
    {
        var optionPile = YgoPlayerPiles.OptionPile(player);
        return optionPile != null && card.Pile == optionPile;
    }

    public static bool TryBuildPayload(CardModel card, Creature? enemyTarget, out NetYgoMonsterMenuCommandAction payload)
    {
        payload = default;
        Player? player = card.Owner;
        if (player?.PlayerCombatState == null)
            return false;

        if (card is MonsterCommandCard mcc)
            mcc.TryResolveSourceMonsterFromStoredPetId();

        switch (card)
        {
            case Exit_Monster_Options:
                payload = new NetYgoMonsterMenuCommandAction
                {
                    Kind = YgoMonsterMenuCommandKind.ExitMonsterOptions,
                    PetCombatId = 0,
                    HasEnemyTarget = false
                };
                return true;

            case Toggle_Die_For_You toggle:
            {
                if (toggle.SourceMonster == null)
                {
                    GD.Print(
                        $"[YgoDuelist][MP] YgoMonsterMenuCommandNetHelper: Toggle_Die_For_You SourceMonster still null after resolve; SourcePetCombatId={toggle.SourcePetCombatId}");
                    return false;
                }
                Creature? pet = FindPetForSourceMonster(player, toggle.SourceMonster);
                if (pet?.CombatId is not uint cid)
                    return false;
                payload = new NetYgoMonsterMenuCommandAction
                {
                    Kind = YgoMonsterMenuCommandKind.ToggleDieForYou,
                    PetCombatId = cid,
                    HasEnemyTarget = false
                };
                return true;
            }

            case Command_Change_Battle_Position changePos:
            {
                if (changePos.SourceMonster == null)
                {
                    GD.Print(
                        $"[YgoDuelist][MP] YgoMonsterMenuCommandNetHelper: ChangeBattlePosition SourceMonster still null after resolve; SourcePetCombatId={changePos.SourcePetCombatId}");
                    return false;
                }
                Creature? pet = FindPetForSourceMonster(player, changePos.SourceMonster);
                if (pet?.CombatId is not uint cid)
                    return false;
                bool hasEnemy = enemyTarget?.CombatId is uint;
                payload = new NetYgoMonsterMenuCommandAction
                {
                    Kind = YgoMonsterMenuCommandKind.ChangeBattlePosition,
                    PetCombatId = cid,
                    HasEnemyTarget = hasEnemy,
                    EnemyTargetCombatId = hasEnemy ? enemyTarget!.CombatId!.Value : 0
                };
                return true;
            }

            default:
                return false;
        }
    }

    private static Creature? FindPetForSourceMonster(Player player, NormalMonsterCard source)
    {
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.HasSourceCard(pet, source))
                return pet;
        }

        return null;
    }
}
