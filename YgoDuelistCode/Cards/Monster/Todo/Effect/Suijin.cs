using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Suijin : EffectMonsterCard
{
    public Suijin()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 25,
            baseDef: 24,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Suijin) };

    private bool ShowConsumableShacklesPlusPowerHover =>
        CurrentUpgradeLevel > 0 || UpgradePreviewType != CardUpgradePreviewType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            if (ShowConsumableShacklesPlusPowerHover)
                yield return HoverTipFactory.FromPower<ConsumableShacklesPlusPower>();
            else
                yield return HoverTipFactory.FromPower<ConsumableShacklesPower>();
        }
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        if (player.Creature == null)
            return;
        if (CurrentUpgradeLevel > 0)
            await PowerCmd.Apply<ConsumableShacklesPlusPower>(duelMonsterPet, 1m, player.Creature, this);
        else
            await PowerCmd.Apply<ConsumableShacklesPower>(duelMonsterPet, 1m, player.Creature, this);
    }
}
