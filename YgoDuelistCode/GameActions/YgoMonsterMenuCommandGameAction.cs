using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>
/// Executes duel monster menu effects that are not wired through <see cref="PlayCardAction"/> because the menu
/// cards are <see cref="CardModel.IsPlayable"/> false.
/// </summary>
public sealed class YgoMonsterMenuCommandGameAction : GameAction
{
    private readonly Player _player;
    private readonly NetYgoMonsterMenuCommandAction _payload;

    public YgoMonsterMenuCommandGameAction(Player player, NetYgoMonsterMenuCommandAction payload)
    {
        _player = player;
        _payload = payload;
    }

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    protected override async Task ExecuteAction()
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        GD.Print(
            $"[YgoDuelist][MP] YgoMonsterMenuCommandGameAction: kind={_payload.Kind} ownerNetId={_player.NetId} petCombatId={_payload.PetCombatId} hasEnemy={_payload.HasEnemyTarget}");

        switch (_payload.Kind)
        {
            case YgoMonsterMenuCommandKind.OpenMonsterOptions:
            {
                Creature? pet = FindPetByCombatId(_player, _payload.PetCombatId);
                if (pet == null)
                {
                    GD.PrintErr(
                        $"[YgoDuelist][MP] YgoMonsterMenuCommandGameAction: OpenMonsterOptions pet not found combatId={_payload.PetCombatId}");
                    return;
                }

                DuelMonsterMonsterOptionsMenu.OpenMonsterOptions(pet);
                return;
            }
            case YgoMonsterMenuCommandKind.ExitMonsterOptions:
                await Exit_Monster_Options.ExecuteExitAsync(_player);
                return;
            case YgoMonsterMenuCommandKind.ToggleDieForYou:
            {
                Creature? pet = FindPetByCombatId(_player, _payload.PetCombatId);
                if (pet == null)
                {
                    GD.PrintErr(
                        $"[YgoDuelist][MP] YgoMonsterMenuCommandGameAction: ToggleDieForYou pet not found combatId={_payload.PetCombatId}");
                    return;
                }

                await Toggle_Die_For_You.ExecuteToggleFromPetAsync(_player, pet);
                return;
            }
            case YgoMonsterMenuCommandKind.ChangeBattlePosition:
            {
                Creature? pet = FindPetByCombatId(_player, _payload.PetCombatId);
                if (pet == null)
                {
                    GD.PrintErr(
                        $"[YgoDuelist][MP] YgoMonsterMenuCommandGameAction: ChangeBattlePosition pet not found combatId={_payload.PetCombatId}");
                    return;
                }

                Creature? enemy = null;
                if (_payload.HasEnemyTarget)
                    enemy = FindCreatureByCombatId(_player, _payload.EnemyTargetCombatId);

                await Command_Change_Battle_Position.ExecuteChangeBattlePositionFromPetAsync(_player, pet, enemy);
                return;
            }
            default:
                GD.PrintErr($"[YgoDuelist][MP] YgoMonsterMenuCommandGameAction: unknown kind {_payload.Kind}");
                return;
        }
    }

    private static Creature? FindPetByCombatId(Player player, uint combatId)
    {
        if (player.PlayerCombatState == null)
            return null;
        return player.PlayerCombatState.Pets.FirstOrDefault(p => p.CombatId == combatId);
    }

    private static Creature? FindCreatureByCombatId(Player player, uint combatId)
    {
        CombatState? cs = player.Creature.CombatState;
        if (cs == null)
            return null;
        return cs.Creatures.FirstOrDefault(c => c.CombatId == combatId);
    }

    public override INetAction ToNetAction()
    {
        return _payload;
    }

    public override string ToString()
    {
        return $"YgoMonsterMenuCommandGameAction owner={_player.NetId} {_payload.Kind} pet={_payload.PetCombatId}";
    }
}
