using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Physical_Double : BaseTrapCard
{
    public Physical_Double()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark | YgoCardPackTags.Trap;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner?.Creature?.CombatState != null
        && Owner.Creature.CombatState.HittableEnemies.Any(e => e.IsAlive);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive || target.Side != CombatSide.Enemy)
            return;

        int atk = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        int def = atk;
        int level = 4;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
            return;

        bool ok = await YgoTokenSummon.TrySpecialSummonTokenAsync<Mirage_Token>(
            Owner,
            choiceContext,
            defensePosition: false,
            m =>
            {
                m.ApplyMirageStats(atk, def, level);
            });

        if (!ok)
            return;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
