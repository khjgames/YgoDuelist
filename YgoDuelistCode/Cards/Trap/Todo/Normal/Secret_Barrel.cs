using System;
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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Secret_Barrel : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 2m),
        };

    public Secret_Barrel()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
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
        typeof(Secret_Barrel),
    };

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var handPile = PileType.Hand.GetPile(Owner);
        int hits = handPile?.Cards?.Count ?? 0;
        if (hits <= 0)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        CombatState cs = Owner.Creature.CombatState;
        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(Owner, this);

        for (int i = 0; i < hits; i++)
        {
            List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0)
                return;

            Creature? victim = YgoDeterministicRng.PickOne(cs, enemies, $"SECRET_BARREL-{Id.Entry}-{i}", mix);
            if (victim == null || !victim.IsAlive)
                continue;

            await CreatureCmd.Damage(choiceContext, victim, dmg, ValueProp.Unpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
    }
}
