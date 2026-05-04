using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

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
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Spellbinding_Circle),
    };

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        await SpellbindingCircleTargetPower.RemoveAllForApplier(Owner.Creature);

        decimal strLoss = DynamicVars["Mgc"].BaseValue;
        decimal spellbound = DynamicVars["Mgc2"].BaseValue;

        if (IsUpgraded)
        {
            await PowerCmd.Apply<SpellbindingTemporaryStrengthPowerPlus>(target, strLoss, Owner.Creature, this);
            await PowerCmd.Apply<SpellboundPlusPower>(target, spellbound, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<SpellbindingTemporaryStrengthPower>(target, strLoss, Owner.Creature, this);
            await PowerCmd.Apply<SpellboundPower>(target, spellbound, Owner.Creature, this);
        }

        await PowerCmd.Apply<SpellbindingCircleTargetPower>(target, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        DynamicVars["Mgc2"].UpgradeValueBy(2m);
    }
}
