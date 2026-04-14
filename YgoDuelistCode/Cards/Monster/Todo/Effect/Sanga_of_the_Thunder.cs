using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Sanga_of_the_Thunder : EffectMonsterCard
{
    public Sanga_of_the_Thunder()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 26,
            baseDef: 22,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Burn | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Sanga_of_the_Thunder) };

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
