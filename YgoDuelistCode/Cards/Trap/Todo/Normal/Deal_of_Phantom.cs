using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Deal_of_Phantom : BaseTrapCard
{
    public override bool UsesCombatHandDescription => true;
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedBlockVar(ValueProp.Unpowered).WithMultiplier(GraveyardMonsterMultiplier)
        };

    public Deal_of_Phantom()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        decimal block = DynamicVars.CalculatedBlock.Calculate(Owner.Creature);
        if (block <= 0m)
            return;

        await CreatureCmd.GainBlock(Owner.Creature, block, default, cardPlay);
    }

    private static decimal GraveyardMonsterMultiplier(CardModel card, Creature? _)
    {
        if (CombatManager.Instance?.IsInProgress != true || card.Owner == null)
            return 0m;
        return GraveyardRelic.GetGraveyardCards(card.Owner).Count(c => c is BaseMonsterCard);
    }

    protected override void OnUpgrade() => DynamicVars.CalculationExtra.UpgradeValueBy(1m);
}
