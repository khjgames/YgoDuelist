using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Curse of Anubis: this Effect Monster cannot Command Attack this turn; its base DEF is treated as 0 for stat calculation.</summary>
public sealed class YgoCurseOfAnubisEffectMonsterPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YGO_CURSE_OF_ANUBIS_EFFECT_MONSTER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-YGO_CURSE_OF_ANUBIS_EFFECT_MONSTER_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        await PowerCmd.Remove(this);
    }
}
