using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class The_Law_of_the_Normal : BaseSpellCard
{
    public The_Law_of_the_Normal()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)?.OfType<BaseMonsterCard>() ?? Enumerable.Empty<BaseMonsterCard>();
        foreach (BaseMonsterCard m in field)
        {
            if (m.YgoCardType != YgoCardType.Monster)
                continue;
            var pet = TributeSummonSelection.ResolvePetForFieldCard(Owner, m);
            if (pet != null)
                await PowerCmd.Apply<StrengthPower>(pet, 3m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
