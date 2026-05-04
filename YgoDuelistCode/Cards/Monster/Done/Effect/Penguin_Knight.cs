using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Penguin_Knight : EffectMonsterCard, IYgoAfterMonsterMovedToGraveyardFromHandOrDraw
{
    public Penguin_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 9,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Penguin_Knight) };

    public async Task OnAfterMovedToGraveyardFromHandOrDrawAsync(Player player, PileType fromPile)
    {
        if (fromPile != PileType.Draw)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (gy == null || draw == null)
            return;

        var gySnapshot = YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards).ToList();
        foreach (CardModel c in gySnapshot)
        {
            if (c.Pile != gy)
                continue;
            await CardPileCmd.Add(c, draw, CardPilePosition.Random, c, false);
        }

        await CardPileCmd.Shuffle(YgoChoiceContexts.Blocking(), player);
    }
}
