using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Airknight_Parshath : EffectMonsterCard
{
    public Airknight_Parshath()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 19,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Airknight_Parshath) };

    public override bool CardShowsSplinterKeyword => true;

    public override bool AttackDealsSplinterDamage => true;

    protected override async Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        await base.OnAfterMonsterAttackHitAsync(choiceContext, cardPlay, attackCommand);
        if (Owner == null)
            return;

        foreach (var r in attackCommand.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || r.UnblockedDamage <= 0)
                continue;
            await CardPileCmd.Draw(choiceContext, 1, Owner);
            break;
        }
    }

}
