using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Cannon_Soldier : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Cannon_Soldier()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 14,
            baseDef: 13,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Machine | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Cannon_Soldier) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-CANNON_SOLDIER.activated_effect.description";

    public bool ActivatedEffectConsumesOncePerTurnSlot => false;

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && IsOtherFieldMonsterAvailable(Owner);

    private static bool IsOtherFieldMonsterAvailable(Player player)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;
        return YgoMpCombatOrder.PetsAny(
            player.PlayerCombatState,
            p =>
                p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is BaseMonsterCard c
                && c is not Cannon_Soldier);
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;

        var candidates = new List<BaseMonsterCard>();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard c)
                continue;
            if (c is Cannon_Soldier)
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
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState?.Pets == null)
            return;

        if (cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        Creature? sourcePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (sourcePet == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out var chosen) || chosen == null)
            return;

        Creature? tributePet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, chosen));
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { chosen }, graveyard, CardPilePosition.Top, chosen, false);

        int blight = (int)source.DynamicVars["Mgc"].BaseValue;
        if (blight > 0)
            await PowerCmd.Apply<BlightPower>(cardPlay.Target, blight, sourcePet, source);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 8m;
    }
}
