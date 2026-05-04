using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Magical_Merchant : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");

    public Magical_Merchant()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 2,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Light | YgoCardPackTags.Insect | YgoCardPackTags.Draw | YgoCardPackTags.Spell | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Magical_Merchant) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-MAGICAL_MERCHANT.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        _ = choiceContext;
        if (self is not Magical_Merchant || Owner?.PlayerCombatState == null)
            return;

        Player player = Owner;
        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (draw == null || hand == null || gy == null)
            return;

        var milledBeforeHit = new List<CardModel>();
        while (!draw.IsEmpty)
        {
            CardModel? top = draw.Cards.Count > 0 ? draw.Cards[0] : null;
            if (top == null)
                break;

            if (top is BaseSpellCard or BaseTrapCard)
            {
                await CardPileCmd.Add(top, hand, CardPilePosition.Top, top, false);
                foreach (CardModel c in milledBeforeHit)
                {
                    if (c.Pile == draw)
                        await CardPileCmd.Add(c, gy, CardPilePosition.Top, c, false);
                }
                return;
            }

            milledBeforeHit.Add(top);
            await CardPileCmd.Add(top, gy, CardPilePosition.Top, top, false);
        }
    }
}
