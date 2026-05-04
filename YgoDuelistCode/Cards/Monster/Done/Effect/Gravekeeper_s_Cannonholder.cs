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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gravekeeper_s_Cannonholder : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Gravekeeper_s_Cannonholder()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 14,
            baseDef: 12,
            baseMgc: 7,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Spell | YgoCardPackTags.Earth | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Gravekeeper_s_Cannonholder) };

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
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GRAVEKEEPER_S_CANNONHOLDER.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner?.PlayerCombatState != null
        && BuildGravekeeperTributeCandidates(Owner).Count > 0;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> candidates = BuildGravekeeperTributeCandidates(player);
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
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature?.CombatState == null || pet == null)
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
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(player.Creature.CombatState))
        {
            if (blight > 0)
                await PowerCmd.Apply<BlightPower>(enemy, blight, pet, source);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 11m;
    }

    private static List<BaseMonsterCard> BuildGravekeeperTributeCandidates(Player player)
    {
        var list = new List<BaseMonsterCard>();
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!p.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is not BaseMonsterCard c)
                continue;
            if (c is Gravekeeper_s_Cannonholder)
                continue;
            if (!c.Id.Entry.Contains("GRAVEKEEPER", StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(c);
        }

        return list;
    }
}
