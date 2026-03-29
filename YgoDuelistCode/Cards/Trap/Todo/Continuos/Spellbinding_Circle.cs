using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Spellbinding_Circle : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 1m),
            new DynamicVar("Mgc2", 1m)
        };

    public Spellbinding_Circle()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Spellbinding_Circle) };

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        decimal strLoss = DynamicVars["Mgc"].BaseValue;
        decimal spellbound = DynamicVars["Mgc2"].BaseValue;
        await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(target, strLoss, Owner.Creature, this);
        await PowerCmd.Apply<SpellboundPower>(target, spellbound, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
