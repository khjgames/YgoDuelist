using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Dian_Keto_the_Cure_Master : BaseSpellCard
{
    private const decimal HealAmount = 6m;

    public Dian_Keto_the_Cure_Master()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyAlly, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Heal;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        if (target.Monster is not DuelMonsterModel)
            return;

        await CreatureCmd.Heal(target, HealAmount);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
