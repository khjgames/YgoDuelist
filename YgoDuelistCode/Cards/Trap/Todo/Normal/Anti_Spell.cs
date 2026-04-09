using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Anti_Spell : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 8m),
            new DynamicVar("Mgc2", 12m)
        };

    protected override int CanonicalEnergyCost => 0;

    protected override bool HasEnergyCostX => true;

    public Anti_Spell()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapCounter)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Spell;

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

        int x = ResolveEnergyXValue();
        decimal flat = DynamicVars["Mgc"].BaseValue;
        decimal per = DynamicVars["Mgc2"].BaseValue;

        await CreatureCmd.GainBlock(Owner.Creature, flat, default, cardPlay);
        for (int i = 0; i < x; i++)
            await CreatureCmd.GainBlock(Owner.Creature, per, default, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(3m);
        DynamicVars["Mgc2"].UpgradeValueBy(2m);
    }
}
