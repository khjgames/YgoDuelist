using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Arcane_Archer_of_the_Forest : EffectMonsterCard
{
    public Arcane_Archer_of_the_Forest()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || !YgoAnnualTracker.TryConsumeAnnual(Owner, "ARCANE_ARCHER_FOREST"))
            return;

        var hand = Owner.PlayerCombatState?.Hand?.Cards;
        if (hand == null)
            return;

        CardModel? spell = hand.FirstOrDefault(c => c is IYgoCard y && y.YgoCardType == YgoCardType.Spell);
        if (spell == null)
            return;

        await CardCmd.Discard(choiceContext, spell);

        if (Type == CardType.Attack && cardPlay.Target != null)
        {
            await DamageCmd.Attack(18m)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
