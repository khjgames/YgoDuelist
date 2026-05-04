using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Bottomless_Trap_Hole : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 15m),
            new DynamicVar("Mgc2", 22m)
        };

    public Bottomless_Trap_Hole()
        : base(cost: 1, cardType: CardType.Attack, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
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
        typeof(Bottomless_Trap_Hole),
    };

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState == null)
                return false;
            return AnyEnemyMeetsAttackThreshold(Owner);
        }
    }

    private bool AnyEnemyMeetsAttackThreshold(Player player)
    {
        Creature? pc = player.Creature;
        if (pc?.CombatState is not CombatState cs)
            return false;
        decimal threshold = DynamicVars["Mgc"].BaseValue;
        return cs.HittableEnemies.Any(e =>
            e.IsAlive
            && (decimal)YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) >= threshold);
    }

    public override bool RefineIsValidTarget(Creature? target, bool vanillaResult)
    {
        if (!vanillaResult || target == null || Owner?.Creature == null)
            return vanillaResult;
        decimal threshold = DynamicVars["Mgc"].BaseValue;
        int incoming = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        return (decimal)incoming >= threshold;
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        int incoming = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        decimal threshold = DynamicVars["Mgc"].BaseValue;
        if ((decimal)incoming < threshold)
            return;

        await CreatureCmd.Damage(choiceContext, target, DynamicVars["Mgc2"].BaseValue, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(-5m);
        DynamicVars["Mgc2"].UpgradeValueBy(11m);
    }
}
