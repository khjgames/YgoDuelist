using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Kuriboh : EffectMonsterCard
{
    public Kuriboh()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 3,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Kuriboh) };

    protected override bool SupportsHandEffectForm => true;
    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;
    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;
    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override PileType GetResultPileType() =>
        IsHandEffectFormActive ? GraveyardPile.CustomType : base.GetResultPileType();

    protected override bool IsPlayable => base.IsPlayable;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            if (Owner?.Creature == null)
                return;
            await PowerCmd.Apply<BufferPower>(Owner.Creature, 1m, Owner.Creature, this);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 2m;
}
