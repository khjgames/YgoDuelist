using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Stealth_Bird : EffectMonsterCard
{
    public Stealth_Bird()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 7,
            baseDef: 17,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Stealth_Bird),
    };

    public override bool UsesFaceDownFlipDamageOnCommandAttack => true;

    /// <summary>Flip Summon / flip-to-attack from face-down defense: magic damage — <see cref="OnCommandAttackAfterStanceSyncedAsync"/>.</summary>
    public static async Task DealFlipSummonDamageIfEligibleAsync(
        PlayerChoiceContext choiceContext,
        Stealth_Bird bird,
        bool wasFaceDownDefenseBeforePositionChange,
        Creature? target,
        Creature? playerCreature)
    {
        if (!wasFaceDownDefenseBeforePositionChange || target == null || !target.IsAlive || playerCreature == null)
            return;

        decimal dmg = bird.DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, playerCreature, bird);
    }

    public override async Task OnCommandAttackAfterStanceSyncedAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature? pet,
        CardPlay cardPlay,
        bool stealthBirdWasFaceDownDefenseBeforeCommandAttack)
    {
        if (!stealthBirdWasFaceDownDefenseBeforeCommandAttack || cardPlay.Target == null)
            return;
        await DealFlipSummonDamageIfEligibleAsync(
            ctx,
            this,
            stealthBirdWasFaceDownDefenseBeforeCommandAttack,
            cardPlay.Target,
            player.Creature);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 15m;
    }
}
