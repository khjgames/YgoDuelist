using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>GY -> deck top via the shared graveyard hook interface.</summary>
public sealed class Cockroach_Knight : EffectMonsterCard, IYgoOnAddedToYgoGraveyardPile
{
    public Cockroach_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Cockroach_Knight) };

    public async Task OnAddedToYgoGraveyardPileAsync(Player owner, CardPile pile)
    {
        CardPile? draw = YgoPlayerPiles.Draw(owner);
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (draw == null || gy == null || !gy.Cards.Contains(this))
            return;

        await CardPileCmd.Add(this, draw, CardPilePosition.Top, this, false);
    }
}
