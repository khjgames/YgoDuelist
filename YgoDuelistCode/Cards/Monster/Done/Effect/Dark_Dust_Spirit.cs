using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. Normal/Tribute Summon or flip face-up: destroy all other face-up duel monsters;
/// gain 1 Conduit per destroyed (and 1 Energy per destroyed when upgraded). End of your turn: return to hand.
/// </summary>
public sealed class Dark_Dust_Spirit : EffectMonsterCard,
    IMonsterFlipEffect,
    IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    public Dark_Dust_Spirit()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 18,
            duelMonsterAttackPlayEnergyOverride: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Zombie;

    public override Type[] RelatedCards => new[] { typeof(Dark_Dust_Spirit) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async ctx => await DestroyOtherFaceUpFieldMonstersAsync(player, duelMonsterPet, ctx, this));

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await RunOnFlipSummonedFromCommandMenuAsync(
            choiceContext,
            async ctx =>
            {
                Creature? pet = player.PlayerCombatState == null
                    ? null
                    : YgoMpCombatOrder.FirstPetWhere(
                        player.PlayerCombatState,
                        p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this));
                await DestroyOtherFaceUpFieldMonstersAsync(player, pet, ctx, this);
            });

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Dark_Dust_Spirit || Owner == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState!,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null)
            return;
        await DestroyOtherFaceUpFieldMonstersAsync(Owner, pet, choiceContext, this);
    }

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        Owner != null && !FaceDown && pet.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(pet, this);

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature pet)
    {
        if (!IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(pet))
            return;
        YgoDuelMonsterBounceToHand.RegisterForHandReturn(pet);
        await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
            YgoDestructionSourceKind.MonsterEffect,
            pet);
    }

    private static async Task DestroyOtherFaceUpFieldMonstersAsync(
        Player actingPlayer,
        Creature? selfPet,
        PlayerChoiceContext _,
        Dark_Dust_Spirit source)
    {
        CombatState? cs = actingPlayer.Creature?.CombatState;
        if (cs == null)
            return;

        var toKill = new List<Creature>();
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            if (p.PlayerCombatState == null)
                continue;
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(p.PlayerCombatState))
            {
                if (!pet.IsAlive || (selfPet != null && ReferenceEquals(pet, selfPet)))
                    continue;
                BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
                if (m == null || m.FaceDown)
                    continue;
                toKill.Add(pet);
            }
        }

        List<Creature> toKillResolved = YgoDuelMonsterDestructionRules
            .FilterPetsForMassKill(toKill, YgoDestructionSourceKind.MonsterEffect)
            .ToList();
        int destroyed = toKillResolved.Count;
        foreach (Creature pet in toKillResolved)
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);

        if (destroyed <= 0)
            return;

        for (int i = 0; i < destroyed; i++)
        {
            await PlayerCmd.GainStars(1, actingPlayer);
            if (source.IsUpgradedOrPreviewActive)
                await PlayerCmd.GainEnergy(1, actingPlayer);
        }
    }
}
