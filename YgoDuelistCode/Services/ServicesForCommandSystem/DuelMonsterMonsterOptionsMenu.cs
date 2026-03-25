using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Builds the duel monster right-click command pile.
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

        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
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

        bool hideDieToggle = MonsterCommandRegistry.TryGet(pet, out var cmdState) && cmdState.DieForYouForced;
        if (!hideDieToggle)
        {
            Toggle_Die_For_You toggle = combatState.CreateCard<Toggle_Die_For_You>(player);
            toggle.InitializeSource(monsterCard);
            commands.Add(toggle);
        }

        if (monsterCard is IMonsterActivatedEffect)
        {
            Activate_Effect activate = combatState.CreateCard<Activate_Effect>(player);
            activate.InitializeSource(monsterCard);
            commands.Add(activate);
        }

        if (monsterCard is IMonsterOptionCommandProvider provider)
        {
            foreach (var extra in provider.BuildExtraMonsterOptionCommands(combatState, player, pet))
            {
                if (extra == null)
                    continue;
                extra.InitializeSource(monsterCard);
                commands.Add(extra);
            }
        }

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
}
