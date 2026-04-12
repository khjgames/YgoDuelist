using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

/// <summary>Base for YGO Token normal monsters (not playable from the deck; only via card effects).</summary>
public abstract class YgoTokenNormalMonster : NormalMonsterCard, IYgoTokenMonster
{
    protected YgoTokenNormalMonster(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace)
    {
    }

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;
}
