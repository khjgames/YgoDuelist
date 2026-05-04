using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary><see cref="UpkeepLifeLossPower"/> on summon. At start of your turn, enemies with attack intent vs you ≥ <c>Mgc</c> get 1 Weak (field-monster hook).</summary>
public sealed class Gora_Turtle : EffectMonsterCard, IYgoTurnStartWeakFromAttackIntent, IYgoOwnerTurnStartFieldMonsterEffect
{
    public Gora_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 11,
            baseDef: 11,
            baseMgc: 19,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water;
    public int AttackIntentWeakThreshold => (int)DynamicVars["Mgc"].BaseValue;

    public bool IsOwnerTurnStartFieldMonsterEffectActive() => !FaceDown;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 13m;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<UpkeepLifeLossPower>();
        }
    }

    public override async Task OnAfterSummonPipelineAsync(Player player, PlayerChoiceContext ctx, Creature pet, bool canAttackThisTurn)
    {
        await PowerCmd.Apply<UpkeepLifeLossPower>(pet, 1m, player.Creature, this);
    }

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (owner?.Creature?.CombatState == null || FaceDown)
            return;
        int threshold = AttackIntentWeakThreshold;
        if (threshold <= 0)
            return;

        var ctx = EnsureBlockingChoiceContext(choiceContext);
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(owner.Creature.CombatState))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, owner.Creature);
            if (intent < threshold)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, 1m, owner.Creature, this);
        }
    }
}
