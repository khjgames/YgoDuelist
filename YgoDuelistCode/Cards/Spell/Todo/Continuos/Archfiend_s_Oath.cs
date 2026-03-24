using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Archfiend_s_Oath : BaseContinuousSpellCard
{
    public Archfiend_s_Oath()
        : base(1, CardRarity.Common, TargetType.Self)
    {
    }

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        await CreatureCmd.Damage(choiceContext, Owner.Creature, 5m, ValueProp.Unpowered, null, this);
        if (Owner != null)
            await YgoSpellCounterService.Add(Owner, 2, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
