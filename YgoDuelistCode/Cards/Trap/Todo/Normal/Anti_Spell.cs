using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Anti_Spell : BaseTrapCard
{
    public Anti_Spell()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapCounter)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoSpellCounterService.GetCount(Owner) >= 2;

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int spellCounters = YgoSpellCounterService.GetCount(Owner);
        if (spellCounters < 2)
            return;

        await YgoSpellCounterService.Remove(Owner, 2, this);

        // Doc semantics: block scales with this card's X cost.
        // Current implementation treats X as the card's printed cost (1 base, 0 upgraded).
        int x = IsUpgraded ? 0 : 1;
        decimal block = 8m + 14m * x;
        await CreatureCmd.GainBlock(Owner.Creature, block, default, cardPlay);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
