using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Graveyard: shuffle into deck and draw through the shared graveyard hook interface.</summary>
public sealed class Outstanding_Dog_Marron : EffectMonsterCard, IYgoOnAddedToYgoGraveyardPile
{
    public Outstanding_Dog_Marron()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 1,
            baseDef: 1,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Outstanding_Dog_Marron) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    public async Task OnAddedToYgoGraveyardPileAsync(Player owner, CardPile pile)
    {
        CardPile? draw = YgoPlayerPiles.Draw(owner);
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (draw == null || gy == null || !gy.Cards.Contains(this))
            return;

        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        await CardPileCmd.Add(this, draw, CardPilePosition.Bottom, this, false);
        await CardPileCmd.ShuffleIfNecessary(ctx, owner);

        int draws = CurrentUpgradeLevel >= 1 ? 2 : 1;
        await CardPileCmd.Draw(ctx, draws, owner);
    }
}
