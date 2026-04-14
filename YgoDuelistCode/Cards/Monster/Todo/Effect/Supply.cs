using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Supply : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString SelectionPrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Supply()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 13,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Fusion;

    public override Type[] RelatedCards => new[] { typeof(Supply) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Supply || Owner == null)
            return;
        CardPile? gy = GraveyardRelic.GetGraveyardPile(Owner);
        CardPile? hand = PileType.Hand.GetPile(Owner);
        if (gy == null || hand == null)
            return;

        List<BaseMonsterCard> candidates = gy.Cards.OfType<BaseMonsterCard>().Take(10).ToList();
        if (candidates.Count == 0)
            return;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates.Cast<CardModel>().ToList(),
            Owner,
            new CardSelectorPrefs(SelectionPrompt, 1, Math.Min(2, candidates.Count)) { Cancelable = true });

        List<CardModel> chosen = selected.Take(2).ToList();
        if (chosen.Count == 0)
            return;
        await CardPileCmd.Add(chosen, hand, CardPilePosition.Top, this, false);
    }
}
