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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Enchanted_Javelin : BaseTrapCard
{
    /// <summary>Divisor for incoming attack damage (heal = incoming / Mgc). Lower Mgc after upgrade = more heal.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 9m) };

    public Enchanted_Javelin()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Heal | YgoCardPackTags.Trap;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Enchanted_Javelin),
    };

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState == null)
                return false;
            return AnyEnemyWithAttackIntent(Owner);
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        int incoming = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        if (incoming <= 0)
            return;

        await CreatureCmd.Heal(Owner.Creature, incoming / DynamicVars["Mgc"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(-2m);

    private static bool AnyEnemyWithAttackIntent(Player player)
    {
        Creature? pc = player.Creature;
        if (pc?.CombatState is not CombatState cs)
            return false;
        return cs.HittableEnemies.Any(e => e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
    }
}
