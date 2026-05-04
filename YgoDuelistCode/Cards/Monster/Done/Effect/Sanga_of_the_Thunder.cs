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
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Sanga_of_the_Thunder : EffectMonsterCard
{
    public Sanga_of_the_Thunder()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 26,
            baseDef: 22,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder)
    {
    }
    
    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Sanga_of_the_Thunder) };

    private bool ShowConsumableShacklesPlusPowerHover => IsUpgradedOrPreviewActive;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            if (ShowConsumableShacklesPlusPowerHover)
                yield return HoverTipFactory.FromPower<ConsumableShacklesPlusPower>();
            else
                yield return HoverTipFactory.FromPower<ConsumableShacklesPower>();
        }
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            () => ApplyConsumableShacklesOnSummonAsync(player, duelMonsterPet));
}
