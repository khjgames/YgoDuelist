using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Physical_Double : BaseTrapCard
{
    public Physical_Double()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Physical_Double), typeof(Mirage_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Mirage_Token));

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner?.Creature?.CombatState != null
        && AnyEnemyWithAttackIntent(Owner)
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && ReactorSlimeSummonGate.AllowsSummonPrintedRace(Owner, DuelMonsterRace.Warrior);

    public override bool RefineIsValidTarget(Creature? target, bool vanillaResult)
    {
        if (!vanillaResult || target == null || Owner?.Creature == null)
            return vanillaResult;
        return YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature) > 0;
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive || target.Side != CombatSide.Enemy)
            return;

        int atk = YgoIntentAttackDamage.GetTotalAttackIntentDamage(target, Owner.Creature);
        if (atk <= 0)
            return;
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

    private static bool AnyEnemyWithAttackIntent(Player player)
    {
        Creature? pc = player.Creature;
        if (pc?.CombatState is not CombatState cs)
            return false;
        return cs.HittableEnemies.Any(e => e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
    }
}
