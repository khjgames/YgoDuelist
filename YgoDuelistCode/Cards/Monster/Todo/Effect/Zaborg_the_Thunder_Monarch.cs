using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Zaborg_the_Thunder_Monarch : EffectMonsterCard
{
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

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Zaborg_the_Thunder_Monarch) };

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

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);

        if (!_hadTributeMaterialsForSummon)
            return;
        if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
            return;
        if (player.Creature?.CombatState == null)
            return;

        var enemies = player.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        Creature? target = null;
        if (_handPlayEnemyTarget != null && enemies.Contains(_handPlayEnemyTarget))
            target = _handPlayEnemyTarget;
        else if (enemies.Count == 1)
            target = enemies[0];
        else
            target = enemies.OrderBy(e => e.CombatId).First();

        int blight = (int)DynamicVars["Mgc"].BaseValue;
        if (blight > 0)
            await PowerCmd.Apply<BlightPower>(target, blight, duelMonsterPet, this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 24m;
    }
}
