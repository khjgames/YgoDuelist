using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Builds the duel monster right-click command pile and can rebuild it when tribute-toggle visibility goes stale.
/// </summary>
public static class DuelMonsterMonsterOptionsMenu
{
    public static void OpenMonsterOptions(Creature pet, bool deferOptionHandSync = false)
    {
        var player = pet.PetOwner;
        if (player?.PlayerCombatState == null)
            return;

        var sourceCard = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
        if (sourceCard is not NormalMonsterCard monsterCard)
            return;

        CardPile optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return;

        optionPile.Clear();

        var combatState = pet.CombatState;
        if (combatState == null)
            return;

        var commands = new List<CardModel>();

        Command_Defend cmdDefend = combatState.CreateCard<Command_Defend>(player);
        cmdDefend.InitializeSource(monsterCard);
        commands.Add(cmdDefend);

        Command_Attack cmdAttack = combatState.CreateCard<Command_Attack>(player);
        cmdAttack.InitializeSource(monsterCard);
        commands.Add(cmdAttack);

        Command_Change_Battle_Position changePos = combatState.CreateCard<Command_Change_Battle_Position>(player);
        changePos.InitializeSource(monsterCard);
        commands.Add(changePos);

        if (TributeMaterialMarkTracker.ShouldShowToggleTributeSacrificeCommand(player))
        {
            Command_Toggle_Tribute_Sacrifice tributeToggle = combatState.CreateCard<Command_Toggle_Tribute_Sacrifice>(player);
            tributeToggle.InitializeSource(monsterCard);
            commands.Add(tributeToggle);
        }

        Toggle_Die_For_You toggle = combatState.CreateCard<Toggle_Die_For_You>(player);
        toggle.InitializeSource(monsterCard);
        commands.Add(toggle);

        Exit_Monster_Options exit = combatState.CreateCard<Exit_Monster_Options>(player);
        exit.InitializeSource(monsterCard);
        commands.Add(exit);

        foreach (var cmd in commands.OfType<MonsterCommandCard>())
            TaskHelper.RunSafely(cmd.SendThisCommandToYgoOptionPile());

        if (deferOptionHandSync)
            YgoOptionHandBridge.RequestDeferredSyncFromOptionPile(player);
        else
            YgoOptionHandBridge.SyncFromOptionPile(player);
    }

    /// <summary>
    /// If the option pile is open for a duel monster and inclusion of <see cref="Command_Toggle_Tribute_Sacrifice"/>
    /// no longer matches <see cref="TributeMaterialMarkTracker.ShouldShowToggleTributeSacrificeCommand"/>, rebuilds the menu.
    /// </summary>
    public static void TryRefreshMonsterOptionsIfTributeToggleStale(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return;
        if (!CombatManager.Instance.IsInProgress)
            return;

        try
        {
            if (!LocalContext.IsMe(player))
                return;
        }
        catch
        {
            return;
        }

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null || optionPile.Cards.Count == 0)
            return;

        var mcc = optionPile.Cards.OfType<MonsterCommandCard>().FirstOrDefault();
        if (mcc?.SourceMonster == null)
            return;

        bool hasToggle = optionPile.Cards.Any(c => c is Command_Toggle_Tribute_Sacrifice);
        bool shouldHave = TributeMaterialMarkTracker.ShouldShowToggleTributeSacrificeCommand(player);
        if (hasToggle == shouldHave)
            return;

        Creature? targetPet = TributeMaterialMarkTracker.FindPetForMaterial(player, mcc.SourceMonster);
        if (targetPet == null || !targetPet.IsAlive)
            return;

        OpenMonsterOptions(targetPet, deferOptionHandSync: true);
    }

    public static void RequestDeferredTryRefresh(Player? player)
    {
        if (player == null)
            return;
        if (!CombatManager.Instance.IsInProgress)
            return;

        try
        {
            if (!LocalContext.IsMe(player))
                return;
        }
        catch
        {
            return;
        }

        var tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;

        var p = player;
        var timer = tree.CreateTimer(0.0);
        timer.Timeout += () => TryRefreshMonsterOptionsIfTributeToggleStale(p);
    }
}
