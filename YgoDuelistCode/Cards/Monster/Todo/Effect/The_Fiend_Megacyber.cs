using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class The_Fiend_Megacyber : EffectMonsterCard
{
    public The_Fiend_Megacyber()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 22,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter;

    public override Type[] RelatedCards => new[]
    {
        typeof(The_Fiend_Megacyber),
    };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    private int? _energyBaseBeforeHandEffectForm;

    protected override void AfterDisplayFormChanged()
    {
        base.AfterDisplayFormChanged();
        if (!IsMutable)
            return;

        if (IsHandEffectFormActive)
        {
            _energyBaseBeforeHandEffectForm ??= EnergyCost.GetWithModifiers(CostModifiers.Local);
            EnergyCost.SetCustomBaseCost(0);
        }
        else if (_energyBaseBeforeHandEffectForm is int saved)
        {
            EnergyCost.SetCustomBaseCost(saved);
            _energyBaseBeforeHandEffectForm = null;
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;
            if (!IsHandEffectFormActive || Owner == null)
                return true;
            return OpponentControlsTwoMoreMonstersThanYou(Owner)
                && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);
        }
    }

    private static bool OpponentControlsTwoMoreMonstersThanYou(Player player)
    {
        if (player.Creature?.CombatState == null)
            return false;

        int enemy = player.Creature.CombatState.HittableEnemies.Count(e => e.IsAlive);
        int yours = DuelMonsterSummon.CountLiveDuelMonsters(player);
        return enemy >= yours + 2;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player?.Creature != null
                && OpponentControlsTwoMoreMonstersThanYou(player)
                && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            {
                await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
                await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            }

            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }
}
