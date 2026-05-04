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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: Tribute 1 face-up "Torpedo Fish" you control; deal damage equal to this monster's ATK to target enemy.</summary>
public sealed class Orca_Mega_Fortress_of_Darkness : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TorpedoPrompt =
        new("cards", "YGODUELIST-ORCA_MEGA_FORTRESS_OF_DARKNESS.activated_effect.selection");

    public Orca_Mega_Fortress_of_Darkness()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 21,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Orca_Mega_Fortress_of_Darkness), typeof(Torpedo_Fish) };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Attack;

    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;

    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-ORCA_MEGA_FORTRESS_OF_DARKNESS.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoMpCombatOrder.PetsAny(
            Owner.PlayerCombatState,
            p =>
                p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceMonster<Torpedo_Fish>(p) != null);

    public override bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner) =>
        IsActivatedEffectAvailable;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        List<Torpedo_Fish> torps = BuildTorpedoCandidates(player);
        if (torps.Count == 0)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareSingleTributeAsync(
            player,
            source,
            torps.Cast<CardModel>().ToList(),
            TorpedoPrompt);
    }

    private static List<Torpedo_Fish> BuildTorpedoCandidates(Player player)
    {
        var list = new List<Torpedo_Fish>();
        if (player.PlayerCombatState == null)
            return list;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<Torpedo_Fish>(pet) is Torpedo_Fish t)
                list.Add(t);
        }

        return list;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out BaseMonsterCard? chosen)
            || chosen is not Torpedo_Fish)
            return;

        Creature? torpPet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState!,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, chosen));
        if (torpPet == null)
            return;

        await CreatureCmd.Kill(torpPet, force: true);

        int dmg = (int)NormalMonsterCard.GetTotalAtkForPreview((NormalMonsterCard)source);
        if (dmg > 0)
        {
            await DamageCmd.Attack(dmg)
                .FromCard(source)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }
}
