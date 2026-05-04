using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While in your Graveyard: once per turn at turn start, heal 1 HP — <see cref="BaseMonsterCard.OnGraveyardRelicOwnerTurnStartWhileInGraveyardAsync"/>.</summary>
public sealed class Darklord_Marie : EffectMonsterCard
{
    public Darklord_Marie()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 17,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Heal;

    public override Type[] RelatedCards => new[] { typeof(Darklord_Marie) };

    public override async Task OnGraveyardRelicOwnerTurnStartWhileInGraveyardAsync(
        PlayerChoiceContext ctx,
        Player player,
        GraveyardRelic relic)
    {
        if (!relic.TryConsumeAnnual("DARKLORD_MARIE_GY"))
            return;
        if (player.Creature != null)
            await CreatureCmd.Heal(player.Creature, 1m);
    }
}
