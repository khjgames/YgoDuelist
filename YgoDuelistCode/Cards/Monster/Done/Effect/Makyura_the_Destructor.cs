using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Makyura_the_Destructor : EffectMonsterCard
{
    public Makyura_the_Destructor()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 12,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Warrior | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Makyura_the_Destructor) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<HastenTrapPower>();
        }
    }

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(GrantHastenTrapAsync(player));
    }

    private async Task GrantHastenTrapAsync(Player player) =>
        await PowerCmd.Apply<HastenTrapPower>(player.Creature, DynamicVars["Mgc"].BaseValue, player.Creature, this);
}
