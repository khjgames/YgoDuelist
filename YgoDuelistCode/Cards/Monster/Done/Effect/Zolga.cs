using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// While this card is in your Graveyard: when you Tribute Summon a monster that used this card as material,
/// heal 2 HP (4 when this copy is upgraded).
/// </summary>
public sealed class Zolga : EffectMonsterCard
{
    public Zolga()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 17,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Heal;

    public override Type[] RelatedCards => new[] { typeof(Zolga) };

    public override async Task OnTributeSummonedMonster(
        PlayerChoiceContext choiceContext,
        Player player,
        BaseMonsterCard summonedMonster,
        IReadOnlyList<BaseMonsterCard> tributeMonsters)
    {
        if (!ReferenceEquals(Owner, player))
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;

        if (!tributeMonsters.Any(m => ReferenceEquals(m, this)))
            return;

        if (player.Creature == null)
            return;

        decimal heal = IsUpgraded ? 4m : 2m;
        await CreatureCmd.Heal(player.Creature, heal);
    }
}
