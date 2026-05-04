using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

/// <summary>Trap: enemies that intend to attack you lose effective damage via Weak.</summary>
public sealed class Amazoness_Archers : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 3m) };

    public Amazoness_Archers()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Warrior;

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
        {
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, Owner.Creature) <= 0)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade(){
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
