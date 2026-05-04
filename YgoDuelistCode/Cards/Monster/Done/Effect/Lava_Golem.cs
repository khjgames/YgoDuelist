using System.Linq;
using System.Threading.Tasks;
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

/// <summary>On Summon: Reckless 4. Activate Effect: Tribute 1 other monster you control; this monster's pet heals {Mgc} HP.</summary>
public sealed class Lava_Golem : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly MegaCrit.Sts2.Core.Localization.LocString TributePrompt =
        new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Lava_Golem()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 30,
            baseDef: 25,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Fiend | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Lava_Golem) };

    public override int GetIntrinsicRecklessCombatSelfDamage() => 4;

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-LAVA_GOLEM.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoMpCombatOrder.PetsAny(
            Owner.PlayerCombatState,
            p =>
                p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is BaseMonsterCard c
                && c is not Lava_Golem);

    public override bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner) => IsActivatedEffectAvailable;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState == null)
            return false;

        var candidates = new System.Collections.Generic.List<BaseMonsterCard>();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard c)
                continue;
            if (c is Lava_Golem || ReferenceEquals(c, source))
                continue;
            candidates.Add(c);
        }

        if (candidates.Count == 0)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareSingleTributeAsync(
            player,
            source,
            candidates.Cast<CardModel>().ToList(),
            TributePrompt);
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        Player? player = source.Owner;
        if (player?.PlayerCombatState == null || player.Creature == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || !selfPet.IsAlive)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out BaseMonsterCard? chosen) || chosen == null)
            return;

        Creature? tributePet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, chosen));
        if (tributePet == null)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        decimal h = source.DynamicVars["Mgc"].BaseValue;
        if (h > 0m)
            await CreatureCmd.Heal(selfPet, h);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 15m;
    }
}
