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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Morphing_Jar : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");

    public Morphing_Jar()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 7,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    protected override bool StumblingBlocksHandSummonInAttackPosition => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-MORPHING_JAR.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        Player? player = Owner;
        if (player?.PlayerCombatState == null)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (hand == null || gy == null)
            return;

        IReadOnlyList<CardModel> snapshot = YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards);
        foreach (CardModel card in snapshot)
        {
            if (card.Pile != hand)
                continue;
            await CardPileCmd.Add(card, gy, CardPilePosition.Top, card, false);
        }

        await CardPileCmd.Draw(choiceContext, 5, player);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
