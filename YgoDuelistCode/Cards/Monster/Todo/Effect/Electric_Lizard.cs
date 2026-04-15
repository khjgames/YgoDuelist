using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When destroyed by battle: the enemy that killed it gains <c>{Mgc}</c> Weak and 2 turns of <c>{Mgc2}</c> temporary Strength down.</summary>
public sealed class Electric_Lizard : EffectMonsterCard
{
    public Electric_Lizard()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 8,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Thunder)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Electric_Lizard),
    };


    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 1m) });

    public override Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true && ctx.CommandState.BattleDamageKillerEnemy != null)
            TaskHelper.RunSafely(ApplyWhenDestroyedByBattleAsync(ctx.Player, this, ctx.CommandState.BattleDamageKillerEnemy));
        return base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    internal static async Task ApplyWhenDestroyedByBattleAsync(Player player, Electric_Lizard card, Creature killer)
    {
        Creature? playerCreature = player.Creature;
        if (playerCreature?.CombatState == null || !killer.IsAlive)
            return;

        decimal weakStacks = card.DynamicVars["Mgc"].BaseValue;
        decimal strDown = card.DynamicVars["Mgc2"].BaseValue;

        await PowerCmd.Apply<WeakPower>(killer, weakStacks, playerCreature, card);
        await PowerCmd.Apply<ElectricLizardStrengthDownPower>(killer, strDown, playerCreature, card);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
        DynamicVars["Mgc2"].BaseValue = 2m;
    }
}
