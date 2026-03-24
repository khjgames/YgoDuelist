using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Equip Spell: target a field monster, then remain face-up in the Spell/Trap zone while attached.
/// </summary>
public abstract class BaseEquipSpellCard : BaseSpellCard
{
    private BaseMonsterCard? _equippedMonster;

    protected BaseEquipSpellCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, rarity, target, DuelMonsterRace.SpellEquip)
    {
    }

    public BaseMonsterCard? EquippedMonster => _equippedMonster;

    internal void SetEquippedMonster(BaseMonsterCard? monster) => _equippedMonster = monster;

    public abstract bool CanEquipTo(BaseMonsterCard target);

    public abstract StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped);

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            // Face-up equips stay in the zone (no re-activation). Set (face-down) equips use YGO-style flip activation.
            if (Pile?.Type == SpellTrapZonePile.CustomType)
            {
                if (!FaceDown || Owner == null)
                    return false;
                return DuelMonsterFieldRegistry
                    .GetFieldMonsters(Owner)
                    .OfType<BaseMonsterCard>()
                    .Any(CanEquipTo);
            }

            if (Pile?.Type != PileType.Hand || Owner == null)
                return true;

            if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, this))
                return false;

            return DuelMonsterFieldRegistry
                .GetFieldMonsters(Owner)
                .OfType<BaseMonsterCard>()
                .Any(CanEquipTo);
        }
    }

    protected sealed override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null || player.Creature == null)
            return;

        if (!EquipSpellPlayPayload.TryTakePending(this, out var targetMonster) || targetMonster == null)
            return;

        if (!CanEquipTo(targetMonster))
            return;

        PrepareSpellForActiveFieldZone();

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await YgoSpellTrapZoneBridge.ActivateEquipSpellAsync(this, targetMonster);

        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }
}
