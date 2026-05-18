using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// With another Pyro on the field: +<c>Mgc</c> ATK/DEF.
/// Start of your turn (and when Summoned or flipped face-up): <c>Mgc2</c> Blight on a random enemy while another Pyro is present.
/// </summary>
public sealed class Solar_Flare_Dragon : EffectMonsterCard, IYgoOwnerTurnStartFieldMonsterEffect, IMonsterFlipEffect
{
    public override int AttackPortionCount => 2;
    public Solar_Flare_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Fire;

    public override Type[] RelatedCards => new[] { typeof(Solar_Flare_Dragon) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 5m);
        }
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        if (!HasOtherPyroOnField(Owner, this))
            return (0, 0);

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (mgc, mgc);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
        DynamicVars["Mgc2"].BaseValue = 7m;
    }

    public bool IsOwnerTurnStartFieldMonsterEffectActive() =>
        Owner != null && !FaceDown && DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this);

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        _ = choiceContext;
        if (owner.PlayerCombatState == null || owner.Creature == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            owner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null)
            return;
        await TryPulseBlightOnceThisOwnerTurnAsync(owner, pet);
    }

    public override async Task OnAfterSummonPipelineAsync(
        Player player,
        PlayerChoiceContext ctx,
        Creature pet,
        bool canAttackThisTurn)
    {
        await base.OnAfterSummonPipelineAsync(player, ctx, pet, canAttackThisTurn);
        if (player.Creature?.CombatState == null)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
            return;
        if (FaceDown)
            return;
        await TryPulseBlightOnceThisOwnerTurnAsync(player, pet);
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        _ = choiceContext;
        if (self is not Solar_Flare_Dragon || Owner?.Creature?.CombatState == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null)
            return;
        await TryPulseBlightOnceThisOwnerTurnAsync(Owner, pet);
    }

    private async Task TryPulseBlightOnceThisOwnerTurnAsync(Player owner, Creature pet)
    {
        if (YgoSolarFlareDragonTurnPulseDedup.AlreadyPulsedThisOwnerTurn(owner, pet))
            return;
        if (!await TryPulseBlightOnRandomEnemyAsync(owner, pet))
            return;
        YgoSolarFlareDragonTurnPulseDedup.NotePulse(owner, pet);
    }

    private async Task<bool> TryPulseBlightOnRandomEnemyAsync(Player owner, Creature pet)
    {
        if (owner.Creature?.CombatState == null || FaceDown || !HasOtherPyroOnField(owner, this))
            return false;
        int blight = (int)DynamicVars["Mgc2"].BaseValue;
        if (blight <= 0)
            return false;
        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(owner.Creature.CombatState);
        if (enemies.Count == 0)
            return false;
        Creature? target = YgoDeterministicRng.PickOne(
            owner.Creature.CombatState,
            enemies,
            "SOLAR_FLARE_DRAGON_BLIGHT",
            (ulong)(pet.CombatId ?? 0u));
        if (target == null)
            return false;
        await PowerCmd.Apply<BlightPower>(target, blight, owner.Creature, this);
        return true;
    }

    private static bool HasOtherPyroOnField(Player player, Solar_Flare_Dragon self)
    {
        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (m == null || ReferenceEquals(m, self))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Pyro)
                return true;
        }

        return false;
    }
}
