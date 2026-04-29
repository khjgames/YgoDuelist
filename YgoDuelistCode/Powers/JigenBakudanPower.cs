using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// FLIP: arms at end of your turn if flipped during your turn; otherwise arms immediately. Next <see cref="AfterPlayerTurnStart"/>: destroy all your duel monsters, then damage each enemy by half (75% if upgraded) of their combined ATK.
/// </summary>
public sealed class JigenBakudanPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers",
        _isPlus
            ? "YGODUELIST-JIGEN_BAKUDAN_PLUS_POWER.title"
            : "YGODUELIST-JIGEN_BAKUDAN_POWER.title");

    public override LocString Description => new("powers",
        _isPlus
            ? "YGODUELIST-JIGEN_BAKUDAN_PLUS_POWER.description"
            : "YGODUELIST-JIGEN_BAKUDAN_POWER.description");

    protected override string SmartDescriptionLocKey =>
        _isPlus
            ? "YGODUELIST-JIGEN_BAKUDAN_PLUS_POWER.smartDescription"
            : "YGODUELIST-JIGEN_BAKUDAN_POWER.smartDescription";

    private bool _isPlus;
    private CardModel? _sourceCard;
    private bool _waitingForPlayerTurnEndToArm;
    private bool _armedForExplode;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _isPlus = cardSource?.IsUpgraded == true;
        _sourceCard = cardSource;

        CombatState? cs = Owner?.CombatState;
        if (cs?.CurrentSide == CombatSide.Player)
            _waitingForPlayerTurnEndToArm = true;
        else
            _armedForExplode = true;

        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        if (_waitingForPlayerTurnEndToArm)
        {
            _waitingForPlayerTurnEndToArm = false;
            _armedForExplode = true;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || !_armedForExplode)
            return;

        _armedForExplode = false;
        await ExplodeAsync(choiceContext);
    }

    private async Task ExplodeAsync(PlayerChoiceContext choiceContext)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            await PowerCmd.Remove(this);
            return;
        }

        Player? player = Owner.Player;
        PlayerCombatState? pcs = player?.PlayerCombatState;
        CombatState? cs = Owner.CombatState;
        if (player == null || pcs == null || cs == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        List<Creature> pets = YgoMpCombatOrder.PetsSnapshotAliveDuelMonstersOrderedByCombatId(pcs);

        int totalAtk = 0;
        foreach (Creature pet in pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard bm)
                totalAtk += (int)NormalMonsterCard.GetTotalAtkForPreview(bm);
        }

        foreach (Creature pet in pets)
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(YgoDestructionSourceKind.TrapEffect, pet);

        int dmgEach = _isPlus ? (totalAtk * 3) / 4 : totalAtk / 2;
        if (dmgEach > 0)
        {
            foreach (Creature e in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
                await CreatureCmd.Damage(choiceContext, e, dmgEach, ValueProp.Unpowered, Owner, _sourceCard);
        }

        await PowerCmd.Remove(this);
    }
}
