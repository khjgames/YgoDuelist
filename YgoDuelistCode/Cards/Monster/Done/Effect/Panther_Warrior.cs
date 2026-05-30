using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Start of your turn: this monster gains Refusal (cannot Command Attack until end of turn).
/// Activate Effect: Tribute 1 other monster; remove Refusal and gain {Mgc} Feast (Necrotic Evolution stacks) on this pet.
/// </summary>
public sealed class Panther_Warrior : EffectMonsterCard,
    IMonsterActivatedEffect,
    IMonsterActivatedEffectPrePlaySelection,
    IYgoOwnerTurnStartFieldMonsterEffect,
    IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Panther_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 20,
            baseDef: 16,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.BeastWarrior,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Panther_Warrior) };

    public bool IsOwnerTurnStartFieldMonsterEffectActive() =>
        Owner != null && !FaceDown && DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this);

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        _ = choiceContext;
        if (owner.Creature == null || FaceDown)
            return;
        PlayerCombatState? pcs = owner.PlayerCombatState;
        if (pcs == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            pcs,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, this) && p.IsAlive);
        if (pet == null)
            return;
        await PowerCmd.Apply<PantherWarriorRefusalPower>(pet, 1m, owner.Creature, this);
    }

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        Owner != null
        && pet.IsAlive
        && DuelMonsterFieldRegistry.HasSourceCard(pet, this)
        && pet.GetPower<PantherWarriorRefusalPower>() != null;

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature pet)
    {
        _ = choiceContext;
        _ = owner;
        if (pet.GetPower<PantherWarriorRefusalPower>() is { } refusal)
            await PowerCmd.Remove(refusal);
    }

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-PANTHER_WARRIOR.activated_effect.description";

    public bool IsActivatedEffectAvailable => IsOtherFieldMonsterAvailable(Owner);

    public override bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner) =>
        IsOtherFieldMonsterAvailable(commandOwner);

    private static bool IsOtherFieldMonsterAvailable(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;

        return YgoMpCombatOrder.PetsAny(
            player.PlayerCombatState,
            p =>
                p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is BaseMonsterCard c
                && c is not Panther_Warrior);
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> candidates = BuildTributeCandidates(player, source);
        if (candidates.Count == 0)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareSingleTributeAsync(
            player,
            source,
            candidates.Cast<CardModel>().ToList(),
            TributePrompt);
    }

    private static List<BaseMonsterCard> BuildTributeCandidates(Player player, NormalMonsterCard source)
    {
        var list = new List<BaseMonsterCard>();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard c)
                continue;
            if (c is Panther_Warrior || ReferenceEquals(c, source))
                continue;
            list.Add(c);
        }

        return list;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = choiceContext;
        _ = cardPlay;
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out BaseMonsterCard? chosen) || chosen == null)
            return;

        Creature? tributePet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, chosen));
        if (tributePet == null || !tributePet.IsAlive)
            return;

        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(player, chosen))
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        if (selfPet.GetPower<PantherWarriorRefusalPower>() is { } refusal)
            await PowerCmd.Remove(refusal);

        int feast = (int)source.DynamicVars["Mgc"].BaseValue;
        if (feast > 0 && player.Creature != null)
            await PowerCmd.Apply<NecroticEvolutionPower>(selfPet, feast, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    public override bool IsCommandAttackPlayable(Player? owner, Creature? pet)
    {
        if (pet?.GetPower<PantherWarriorRefusalPower>() != null && DuelMonsterFieldRegistry.HasSourceCard(pet, this))
            return false;
        return base.IsCommandAttackPlayable(owner, pet);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
