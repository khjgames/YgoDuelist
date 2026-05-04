using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Zaborg_the_Thunder_Monarch : EffectMonsterCard
{
    public override int AttackPortionCount => 2;
    private Creature? _handPlayEnemyTarget;
    private bool _hadTributeMaterialsForSummon;

    public Zaborg_the_Thunder_Monarch()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 24,
            baseDef: 10,
            baseMgc: 16,
            duelMonsterRace: DuelMonsterRace.Thunder)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Zaborg_the_Thunder_Monarch) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    /// <inheritdoc cref="BaseMonsterCard.NonAttackPlayTargetType" />
    /// <remarks>Tribute summon from skill (defense) stance still needs an enemy target for the optional blight.</remarks>
    protected override TargetType NonAttackPlayTargetType => TargetType.AnyEnemy;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _handPlayEnemyTarget = cardPlay.Target;
        try
        {
            await base.OnPlay(choiceContext, cardPlay);
        }
        finally
        {
            _handPlayEnemyTarget = null;
        }
    }

    protected override void OnBeforeDuelMonsterSummon(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution? tributePending)
    {
        _hadTributeMaterialsForSummon = tributePending != null
            && (tributePending.Pets.Count > 0 || tributePending.MausoleumHpTributes > 0);
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async () =>
            {
                if (!_hadTributeMaterialsForSummon)
                    return;
                if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
                    return;
                if (player.Creature?.CombatState == null)
                    return;

                var enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(player.Creature.CombatState);
                if (enemies.Count == 0)
                    return;

                Creature? target = null;
                if (_handPlayEnemyTarget != null && enemies.Contains(_handPlayEnemyTarget))
                    target = _handPlayEnemyTarget;
                else if (enemies.Count == 1)
                    target = enemies[0];
                else
                    target = YgoMpCombatOrder.CreatureListOrderedByCombatId(enemies)[0];

                int blight = (int)DynamicVars["Mgc"].BaseValue;
                if (blight > 0)
                    await PowerCmd.Apply<BlightPower>(target, blight, duelMonsterPet, this);
            });

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 24m;
    }
}
