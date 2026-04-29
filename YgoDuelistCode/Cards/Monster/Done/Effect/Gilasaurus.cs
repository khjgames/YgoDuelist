using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gilasaurus : EffectMonsterCard
{
    public Gilasaurus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 4,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Dinosaur)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Gilasaurus) };

    protected override bool SupportsHandEffectForm => true;
    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;
    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;
    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;
    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable
        && (!IsHandEffectFormActive
            || (Owner != null && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            if (Owner?.Creature?.CombatState == null)
                return;
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(Owner, this, choiceContext);
            foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(Owner.Creature.CombatState.HittableEnemies))
            {
                if (!enemy.IsAlive)
                    continue;
                await CreatureCmd.Heal(enemy, 7m);
            }

            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 0m;
}
