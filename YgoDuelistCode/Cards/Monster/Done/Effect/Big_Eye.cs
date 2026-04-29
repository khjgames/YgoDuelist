using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>FLIP: Look at up to 5 cards from the top of your draw pile, then place them on top in any order.</summary>
public sealed class Big_Eye : EffectMonsterCard, IMonsterFlipEffect
{
    public Big_Eye()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Big_Eye) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Big_Eye || Owner == null)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(Owner);
        if (draw == null || draw.Cards.Count == 0)
            return;

        List<CardModel> top = YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards).Take(5).ToList();
        if (top.Count == 0)
            return;

        var remaining = top.ToList();
        var chosenTopToBottom = new List<CardModel>(remaining.Count);

        while (remaining.Count > 0)
        {
            CardModel? pick;
            if (remaining.Count == 1)
            {
                pick = remaining[0];
            }
            else
            {
                try
                {
                    pick = await CardSelectCmd.FromChooseACardScreen(choiceContext, remaining, Owner, canSkip: false);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            if (pick == null)
                return;
            remaining.Remove(pick);
            chosenTopToBottom.Add(pick);
        }

        for (int i = chosenTopToBottom.Count - 1; i >= 0; i--)
            await CardPileCmd.Add(new[] { chosenTopToBottom[i] }, draw, CardPilePosition.Top, this, false);
    }
}
