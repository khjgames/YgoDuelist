using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;

public sealed class Sogen : BaseFieldSpellCard
{
    public Sogen()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target)
    {
        DuelMonsterRace r = target.DuelMonsterRace;
        if (r is DuelMonsterRace.Warrior or DuelMonsterRace.BeastWarrior)
            return new StatEffectTotal(200, 200);
        return StatEffectTotal.None;
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
