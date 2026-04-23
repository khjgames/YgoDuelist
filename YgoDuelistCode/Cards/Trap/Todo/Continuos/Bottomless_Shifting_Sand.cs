using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Bottomless_Shifting_Sand : BaseContinuousTrapCard, IYgoOwnerBeforeTurnEndFlushSpellTrapZoneEffect
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 4m),
            new DynamicVar("Mgc2", 25m)
        };

    public Bottomless_Shifting_Sand()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Bottomless_Shifting_Sand),
    };

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(-1m);
        DynamicVars["Mgc2"].UpgradeValueBy(15m);
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    public bool IsOwnerBeforeTurnEndFlushSpellTrapZoneEffectActive() =>
        !FaceDown && Owner != null;

    public async Task TryResolveOwnerBeforeTurnEndFlushSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (!IsOwnerBeforeTurnEndFlushSpellTrapZoneEffectActive() || Owner != owner || owner.Creature == null)
            return;

        int handThreshold = (int)DynamicVars["Mgc"].BaseValue;
        CardPile? hand = YgoPlayerPiles.Hand(owner);
        int handCount = hand?.Cards.Count ?? 0;
        if (handCount < handThreshold)
        {
            CardPile? gy = YgoPlayerPiles.Graveyard(owner);
            if (gy != null)
                await CardPileCmd.Add(new[] { this }, gy, CardPilePosition.Top, this, false);
            YgoSpellTrapZoneBridge.SyncFromZonePile(owner);
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(owner);
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(owner);
            return;
        }

        var cs = owner.Creature.CombatState;
        if (cs == null)
            return;
        List<Creature> alive = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
        if (alive.Count == 0)
            return;

        Creature? best = null;
        int bestIntent = -1;
        foreach (Creature e in YgoDeterministicRng.StableOrder(alive, c => c.CombatId))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, owner.Creature);
            if (intent > bestIntent)
            {
                bestIntent = intent;
                best = e;
            }
        }

        if (best == null || bestIntent <= 0)
            return;
        decimal cap = DynamicVars["Mgc2"].BaseValue;
        decimal dmg = Math.Min(bestIntent, cap);
        await CreatureCmd.Damage(choiceContext, best, dmg, ValueProp.Unpowered, owner.Creature, this);
    }
}
