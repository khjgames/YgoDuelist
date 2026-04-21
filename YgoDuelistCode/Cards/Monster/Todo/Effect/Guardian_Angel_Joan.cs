using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>On execute kill: heal <c>Mgc</c> + 2% of that enemy's max HP.</summary>
public sealed class Guardian_Angel_Joan : EffectMonsterCard
{
    public Guardian_Angel_Joan()
        : base(
            cost: 1,
            type: global::MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack,
            rarity: global::MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Uncommon,
            target: global::MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 20,
            baseMgc: 1,
            duelMonsterRace: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Heal;

    public override Type[] RelatedCards => new[] { typeof(Guardian_Angel_Joan) };

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        Player? atkPlayer = command.Attacker.Player;
        if (atkPlayer?.Creature == null)
            return;
        foreach (DamageResult r in command.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                continue;
            decimal mgc = DynamicVars["Mgc"].BaseValue;
            decimal pct = r.Receiver.MaxHp * 0.02m;
            await CreatureCmd.Heal(atkPlayer.Creature, mgc + pct);
            break;
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
