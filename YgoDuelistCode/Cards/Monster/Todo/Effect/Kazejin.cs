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

public sealed class Kazejin : EffectMonsterCard
{
    public Kazejin()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 24,
            baseDef: 22,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Wind | YgoCardPackTags.Spellcaster | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Kazejin) };

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
