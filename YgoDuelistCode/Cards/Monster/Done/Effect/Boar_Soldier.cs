using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Boar_Soldier : EffectMonsterCard
{
    public Boar_Soldier()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 20,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Boar_Soldier) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner?.Creature?.CombatState == null)
            return base.GetSecondaryStats();

        bool enemyHasMonster = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState).Count > 0;
        if (!enemyHasMonster)
            return base.GetSecondaryStats();

        return (-10, 0);
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async _ =>
            {
                if (duelMonsterPet.IsAlive)
                    await CreatureCmd.Kill(duelMonsterPet, force: true);
            });
    }
}
