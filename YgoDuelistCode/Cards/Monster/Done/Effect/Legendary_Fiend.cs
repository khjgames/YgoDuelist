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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Legendary_Fiend : EffectMonsterCard, IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset
{
    public override int AttackPortionCount => 3;
    public Legendary_Fiend()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 18,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Fiend | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[] { typeof(Legendary_Fiend) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<LegendaryFiendAtkPower>();
        }
    }

    public async Task ApplyTurnStartAtkGrowthAsync(Player player, Creature pet)
    {
        LegendaryFiendAtkPower? p = pet.GetPower<LegendaryFiendAtkPower>();
        if (p == null)
            await PowerCmd.Apply<LegendaryFiendAtkPower>(pet, 7m, player.Creature, null);
        else
            await PowerCmd.ModifyAmount(p, this.DynamicVars["Mgc"].BaseValue, player.Creature, null);
    }
}
