using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Dark_Snake_Syndrome : BaseContinuousSpellCard
{
    public Dark_Snake_Syndrome()
        : base(3, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive || !target.CombatId.HasValue)
            return;

        Creature creature = Owner.Creature;
        await DarkSnakeSyndromeFieldPower.RemoveAllForApplier(creature);

        DarkSnakeSyndromeFieldPower? existing = target.GetPower<DarkSnakeSyndromeFieldPower>();
        if (existing != null)
            await PowerCmd.Remove(existing);

        await PowerCmd.Apply<DarkSnakeSyndromeFieldPower>(target, 1m, creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
