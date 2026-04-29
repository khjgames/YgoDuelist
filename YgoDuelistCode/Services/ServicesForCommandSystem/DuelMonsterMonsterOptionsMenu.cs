using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Builds the duel monster right-click command pile.
/// </summary>
public static class DuelMonsterMonsterOptionsMenu
{
    /// <summary>
    /// Fills <see cref="YgoCardOptionPile"/> synchronously so every option card is registered in
    /// <see cref="NetCombatCardDb"/> before the current <see cref="MegaCrit.Sts2.Core.GameActions.GameAction"/> returns.
    /// Returns <see cref="Task.CompletedTask"/> only so callers may <c>await</c> without yielding the Godot main loop.
    /// </summary>
    public static Task OpenMonsterOptionsAsync(Creature pet, bool deferOptionHandSync = false)
    {
        var player = pet.PetOwner;
        if (player?.PlayerCombatState == null)
            return Task.CompletedTask;

        var sourceCard = DuelMonsterFieldRegistry.GetSourceMonster<NormalMonsterCard>(pet);
        if (sourceCard is not NormalMonsterCard monsterCard)
            return Task.CompletedTask;

        CardPile optionPile = YgoPlayerPiles.OptionPile(player);
        if (optionPile == null)
            return Task.CompletedTask;

        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
        optionPile.Clear();

        var combatState = pet.CombatState;
        if (combatState == null)
            return Task.CompletedTask;

        var commands = new List<CardModel>();

        Command_Defend cmdDefend = combatState.CreateCard<Command_Defend>(player);
        cmdDefend.InitializeSource(monsterCard, pet);
        commands.Add(cmdDefend);

        if (!monsterCard.DuelMonsterExcludesCommandAttack)
        {
            Command_Attack cmdAttack = combatState.CreateCard<Command_Attack>(player);
            cmdAttack.InitializeSource(monsterCard, pet);
            commands.Add(cmdAttack);
        }

        Command_Change_Battle_Position changePos = combatState.CreateCard<Command_Change_Battle_Position>(player);
        changePos.InitializeSource(monsterCard, pet);
        commands.Add(changePos);

        bool hideDieToggle = MonsterCommandRegistry.TryGet(pet, out var cmdState) && cmdState.DieForYouForced;
        if (!hideDieToggle)
        {
            Toggle_Die_For_You toggle = combatState.CreateCard<Toggle_Die_For_You>(player);
            toggle.InitializeSource(monsterCard, pet);
            commands.Add(toggle);
        }

        if (monsterCard is IMonsterActivatedEffect)
        {
            Activate_Effect activate = combatState.CreateCard<Activate_Effect>(player);
            activate.InitializeSource(monsterCard, pet);
            commands.Add(activate);
        }

        if (monsterCard is IMonsterSecondActivatedEffect)
        {
            Activate_Effect_2 activate2 = combatState.CreateCard<Activate_Effect_2>(player);
            activate2.InitializeSource(monsterCard, pet);
            commands.Add(activate2);
        }

        if (pet.GetPower<ConsumableShacklesPower>() is ConsumableShacklesPower shacklesPow && shacklesPow.Amount >= 1m)
        {
            Activate_Shackles shackles = combatState.CreateCard<Activate_Shackles>(player);
            shackles.InitializeSource(monsterCard, pet);
            commands.Add(shackles);
        }

        if (pet.GetPower<ConsumableShacklesPlusPower>() is ConsumableShacklesPlusPower shacklesPlusPow && shacklesPlusPow.Amount >= 1m)
        {
            Activate_Shackles_Plus shacklesPlus = combatState.CreateCard<Activate_Shackles_Plus>(player);
            shacklesPlus.InitializeSource(monsterCard, pet);
            commands.Add(shacklesPlus);
        }

        if (Egyptian_God_Slime.PlayerHasSlimeInExtraDeck(player)
            && monsterCard is BaseMonsterCard bm
            && Egyptian_God_Slime.QualifiesAsSlimeTributeMaterial(bm))
        {
            Special_Summon_Egyptian_God_Slime slimeCmd = combatState.CreateCard<Special_Summon_Egyptian_God_Slime>(player);
            slimeCmd.InitializeSource(monsterCard, pet);
            commands.Add(slimeCmd);
        }

        if (monsterCard is Labyrinth_Wall labyrinthWall
            && Magical_Labyrinth.IsFaceUpEquippedTo(labyrinthWall)
            && Special_Summon_Wall_Shadow.BuildWallShadowHandOrDeckCandidates(player).Count > 0
            && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 1))
        {
            Special_Summon_Wall_Shadow wallShadowCmd = combatState.CreateCard<Special_Summon_Wall_Shadow>(player);
            wallShadowCmd.InitializeSource(monsterCard, pet);
            commands.Add(wallShadowCmd);
        }

        if (monsterCard is Dark_Magician darkMagician
            && darkMagician.SurvivedTimeMagic
            && Special_Summon_Dark_Sage.BuildDarkSageHandOrDeckCandidates(player).Count > 0
            && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 1))
        {
            Special_Summon_Dark_Sage darkSageCmd = combatState.CreateCard<Special_Summon_Dark_Sage>(player);
            darkSageCmd.InitializeSource(monsterCard, pet);
            commands.Add(darkSageCmd);
        }

        if (monsterCard is BaseMonsterCard material
            && Xy_Dragon_Cannon.PlayerHasInExtraDeck(player)
            && Xy_Dragon_Cannon.TryGetExactFieldMaterials(player, out var xyMats)
            && xyMats.Any(m => ReferenceEquals(m, material)))
        {
            Special_Summon_XY_Dragon_Cannon cmd = combatState.CreateCard<Special_Summon_XY_Dragon_Cannon>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is BaseMonsterCard materialXz
            && Xz_Tank_Cannon.PlayerHasInExtraDeck(player)
            && Xz_Tank_Cannon.TryGetExactFieldMaterials(player, out var xzMats)
            && xzMats.Any(m => ReferenceEquals(m, materialXz)))
        {
            Special_Summon_XZ_Tank_Cannon cmd = combatState.CreateCard<Special_Summon_XZ_Tank_Cannon>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is BaseMonsterCard materialYz
            && Yz_Tank_Dragon.PlayerHasInExtraDeck(player)
            && Yz_Tank_Dragon.TryGetExactFieldMaterials(player, out var yzMats)
            && yzMats.Any(m => ReferenceEquals(m, materialYz)))
        {
            Special_Summon_YZ_Tank_Dragon cmd = combatState.CreateCard<Special_Summon_YZ_Tank_Dragon>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is BaseMonsterCard materialVw
            && Vw_Tiger_Catapult.PlayerHasInExtraDeck(player)
            && Vw_Tiger_Catapult.TryGetExactFieldMaterials(player, out var vwMats)
            && vwMats.Any(m => ReferenceEquals(m, materialVw)))
        {
            Special_Summon_VW_Tiger_Catapult cmd = combatState.CreateCard<Special_Summon_VW_Tiger_Catapult>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is BaseMonsterCard materialXyz
            && Xyz_Dragon_Cannon.PlayerHasInExtraDeck(player)
            && Xyz_Dragon_Cannon.TryGetExactFieldMaterials(player, out var xyzMats)
            && xyzMats.Any(m => ReferenceEquals(m, materialXyz)))
        {
            Special_Summon_XYZ_Dragon_Cannon cmd = combatState.CreateCard<Special_Summon_XYZ_Dragon_Cannon>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is BaseMonsterCard materialVwxyz
            && Vwxyz_Dragon_Catapult_Cannon.PlayerHasInExtraDeck(player)
            && Vwxyz_Dragon_Catapult_Cannon.TryGetExactFieldMaterials(player, out var vwxyzMats)
            && vwxyzMats.Any(m => ReferenceEquals(m, materialVwxyz)))
        {
            Special_Summon_VWXYZ_Dragon_Catapult_Cannon cmd = combatState.CreateCard<Special_Summon_VWXYZ_Dragon_Catapult_Cannon>(player);
            cmd.InitializeSource(monsterCard, pet);
            commands.Add(cmd);
        }

        if (monsterCard is IMonsterOptionCommandProvider provider)
        {
            foreach (var extra in provider.BuildExtraMonsterOptionCommands(combatState, player, pet))
            {
                if (extra == null)
                    continue;
                extra.InitializeSource(monsterCard, pet);
                commands.Add(extra);
            }
        }

        Exit_Monster_Options exit = combatState.CreateCard<Exit_Monster_Options>(player);
        exit.InitializeSource(monsterCard, pet);
        commands.Add(exit);

        foreach (MonsterCommandCard cmd in commands.OfType<MonsterCommandCard>())
            cmd.SendThisCommandToYgoOptionPile();

        if (deferOptionHandSync)
            YgoOptionHandBridge.RequestDeferredSyncFromOptionPile(player);
        else
            YgoOptionHandBridge.SyncFromOptionPile(player);

        var db = NetCombatCardDb.Instance;
        var parts = new List<string>(optionPile.Cards.Count);
        foreach (CardModel c in optionPile.Cards)
        {
            string idStr = db.TryGetCardId(c, out uint id) ? id.ToString() : "?";
            parts.Add($"{c.Id?.Entry ?? "?"}={idStr}");
        }

        GD.Print(
            $"[YgoDuelist][MP][OptionPile] OpenMonsterOptions: player={player.NetId} count={optionPile.Cards.Count} ids=[{string.Join(", ", parts)}]");

        return Task.CompletedTask;
    }

    /// <summary>Right-click UI: same synchronous populate as MP (no TaskHelper — avoids interleaving).</summary>
    public static void OpenMonsterOptions(Creature pet, bool deferOptionHandSync = false) =>
        _ = OpenMonsterOptionsAsync(pet, deferOptionHandSync);
}
