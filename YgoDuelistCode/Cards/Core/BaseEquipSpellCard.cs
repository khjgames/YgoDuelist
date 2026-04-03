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

    /// <summary>Extra discount applied only when the equipped monster pays attack-stance / Command Attack energy.</summary>
    public virtual int GetEquipAttackPlayEnergyDiscount(BaseMonsterCard equipped) => 0;

    /// <summary>Extra discount applied only when the equipped monster pays defense-stance / Command Defend energy.</summary>
    public virtual int GetEquipDefensePlayEnergyDiscount(BaseMonsterCard equipped) => 0;

    /// <summary>Extra self-damage to the duel pet before each attack or block combat action (stacks with level-based reckless).</summary>
    public virtual int GetEquipRecklessCombatSelfDamage(BaseMonsterCard equipped) => 0;

    /// <summary>Multiplies summed ATK/DEF after flat <see cref="GetEquipStatEffect"/> from this equip (and other equips' flat bonuses) are applied.</summary>
    public virtual StatEffectTotalMultiplier GetEquipStatMultiplier(BaseMonsterCard equipped) => StatEffectTotalMultiplier.Identity;

    /// <summary>When true, the equipped monster's attacks also resolve Splinter splash (see <see cref="Relics.GraveyardRelic"/>).</summary>
    public virtual bool GrantsSplinterDamage => false;

    /// <summary>When true, the equipped monster's attacks also apply Blight from unblocked damage (see <see cref="Relics.GraveyardRelic"/>).</summary>
    public virtual bool GrantsBlightedDamage => false;

    /// <summary>Per-equip override: Splinter for this attachment (default: <see cref="GrantsSplinterDamage"/>).</summary>
    public virtual bool GrantsSplinterTo(BaseMonsterCard equipped) => GrantsSplinterDamage;

    /// <summary>Per-equip override: Blighted attacks for this attachment (default: <see cref="GrantsBlightedDamage"/>).</summary>
    public virtual bool GrantsBlightTo(BaseMonsterCard equipped) => GrantsBlightedDamage;

    protected virtual bool CardShowsSplinterKeywordHint => GrantsSplinterDamage;

    protected virtual bool CardShowsBlightKeywordHint => GrantsBlightedDamage;

    public override bool CardShowsSplinterKeyword => CardShowsSplinterKeywordHint;

    public override bool CardShowsBlightKeyword => CardShowsBlightKeywordHint;

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

        await YgoCurseOfDarknessSpellHook.AfterSpellResolved(choiceContext, this);

        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }
}
