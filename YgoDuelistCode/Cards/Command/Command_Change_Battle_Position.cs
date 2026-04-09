using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Unplayable command option: switching battle position runs via the same "cannot play" path as
/// <see cref="Toggle_Die_For_You"/>. Title shows the next action (attack vs defense) based on current position.
/// Applies <see cref="StiffPower"/> only (no <see cref="FatiguePower"/>, no command-slot consumption).
/// <see cref="Stealth_Bird"/>: requires an enemy target when flip-changing from face-down defense to attack
/// to apply <c>Mgc</c> damage (see <see cref="TargetType"/>).
/// </summary>
public sealed class Command_Change_Battle_Position : MonsterCommandCard
{
    public Command_Change_Battle_Position()
    {
    }

    public Command_Change_Battle_Position(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    /// <summary>
    /// <see cref="Stealth_Bird"/> in face-down defense is about to flip to attack: enemy target for <c>Mgc</c> damage.
    /// All other positions use Self (attack→defense, face-up defense→attack).
    /// </summary>
    public override TargetType TargetType =>
        SourceMonster is Stealth_Bird sb && !sb.IsAttackBattlePosition && sb.FaceDown
            ? TargetType.AnyEnemy
            : TargetType.Self;

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        "YgoDuelist/images/card_frames/Invisible_Energy.png";

    /// <summary>Shared by UI click and <see cref="GameActions.YgoMonsterMenuCommandGameAction"/> (MP).</summary>
    public static async Task ExecuteChangeBattlePositionFromPetAsync(Player player, Creature pet, Creature? enemyTarget)
    {
        if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not NormalMonsterCard sourceMonster)
            return;
        if (sourceMonster is not AbstractMonsterCard monster)
            return;

        if (pet.HasPower<StiffPower>())
            return;

        bool wasAttackPosition = monster.IsAttackBattlePosition;
        bool wasFaceDownDefense =
            sourceMonster is Stealth_Bird
            && !monster.IsAttackBattlePosition
            && monster.FaceDown;

        bool switchedDefToAtk = monster.ApplyBattlePositionChangeFromCommandMenu();

        // UpdateFaceDownKeywordFromBool queues stance sync asynchronously; GameAction must finish after powers match card state.
        await DuelMonsterStancePowerSync.SyncForPetAsync(pet, monster, player.Creature, sourceMonster);

        // Do not `new Command_Change_Battle_Position(...)` — runtime construction throws DuplicateModelException
        // ("Use ModelDb instead"). Use the field monster card as the power source (same logical source as menu `this`).
        await MonsterCommandRegistry.ApplyStiffFromBattlePositionChangeOnly(pet, player.Creature, sourceMonster);

        var ctx = new BlockingPlayerChoiceContext();
        if (switchedDefToAtk)
            await monster.OnSwitchedFromDefenseToAttackFromCommandAsync(ctx, player);
        else if (wasAttackPosition)
            await monster.OnSwitchedFromAttackToDefenseFromCommandAsync(ctx, player);

        if (sourceMonster is Stealth_Bird bird && wasFaceDownDefense && switchedDefToAtk)
            await Stealth_Bird.DealFlipSummonDamageIfEligibleAsync(ctx, bird, wasFaceDownDefense, enemyTarget, player.Creature);

        YgoOptionHandBridge.RequestDeferredSyncFromOptionPile(player);
    }

    public async Task OnClickedOption(Creature? enemyTarget = null)
    {
        var player = Owner;
        if (player == null || SourceMonster is not AbstractMonsterCard)
            return;

        var pet = FindPetForMonster(SourceMonster, player);
        if (pet == null)
            return;

        await ExecuteChangeBattlePositionFromPetAsync(player, pet, enemyTarget);
    }

    private static Creature? FindPetForMonster(NormalMonsterCard source, Player player)
    {
        if (player.PlayerCombatState == null)
            return null;

        return player.PlayerCombatState.Pets
            .FirstOrDefault(p => p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source);
    }
}
