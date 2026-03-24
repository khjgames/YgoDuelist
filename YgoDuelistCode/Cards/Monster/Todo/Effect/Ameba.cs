using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Ameba : EffectMonsterCard
{
    public Ameba()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 3,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || !YgoAnnualTracker.TryConsumeAnnual(Owner, "AMEBA_GROW"))
            return;

        var pet = TributeSummonSelection.ResolvePetForFieldCard(Owner, this);
        if (pet != null && Owner.Creature != null)
            await PowerCmd.Apply<StrengthPower>(pet, 4m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
