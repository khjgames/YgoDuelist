using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Berserk_Dragon : EffectMonsterCard, IYgoOwnerTurnStartFieldMonsterEffect
{
    public Berserk_Dragon()
        : base(
            cost: 2,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 35,
            baseDef: 0,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Zombie,
            duelMonsterAttackPlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] BundledCards => new[] { typeof(A_Deal_with_Dark_Ruler) };

    public override bool CanSummonDuelMonster => false;

    protected override bool RegistersForLevel8DealWithDarkRulerWhenDestroyed => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoDealWithDarkRulerState.IsDealWithDarkRulerSummonBypassActive;

    public override bool DuelMonsterAttackHitsAllEnemies => true;

    protected override bool IsPlayable => base.IsPlayable && CanSummonDuelMonster;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }

    public bool IsOwnerTurnStartFieldMonsterEffectActive() => !FaceDown;

    public Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        _ = choiceContext;
        _ = owner;
        if (DynamicVars?.Damage == null || !DynamicVars.ContainsKey("Mgc"))
            return Task.CompletedTask;

        decimal loss = DynamicVars["Mgc"].BaseValue;
        if (loss <= 0m)
            return Task.CompletedTask;
        decimal next = DynamicVars.Damage.BaseValue - loss;
        if (next < 0m)
            next = 0m;
        DynamicVars.Damage.BaseValue = next;
        return Task.CompletedTask;
    }
}
