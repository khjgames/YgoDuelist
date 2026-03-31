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

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Dark_Snake_Syndrome : BaseContinuousSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 32m) };

    public Dark_Snake_Syndrome()
        : base(3, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Dark_Snake_Syndrome),
    };

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

        if (target.GetPower<DarkSnakeSyndromeFieldPower>() is { } oldBase)
            await PowerCmd.Remove(oldBase);
        if (target.GetPower<DarkSnakeSyndromeFieldPowerPlus>() is { } oldPlus)
            await PowerCmd.Remove(oldPlus);

        if (IsUpgraded)
            await PowerCmd.Apply<DarkSnakeSyndromeFieldPowerPlus>(target, 1m, creature, this);
        else
            await PowerCmd.Apply<DarkSnakeSyndromeFieldPower>(target, 1m, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(32m);
    }
}
