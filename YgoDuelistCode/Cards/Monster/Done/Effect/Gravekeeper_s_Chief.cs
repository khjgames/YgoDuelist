using System;
using System.Collections.Generic;
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
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: Special Summon 1 Gravekeeper's monster from your Graveyard; gain {Mgc} Block (1 Energy, 1 Conduit).</summary>
public sealed class Gravekeeper_s_Chief : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString GyPrompt = new("cards", "YGODUELIST-GRAVEKEEPER_S_CHIEF.gy_summon_select");

    public Gravekeeper_s_Chief()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 19,
            baseDef: 12,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Gravekeeper_s_Chief) };

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GRAVEKEEPER_S_CHIEF.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && BuildGraveyardTargets(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = BuildGraveyardTargets(player);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? summon = candidates.Count == 1
            ? candidates[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(GyPrompt, 1, 1) { Cancelable = true },
                () => BuildGraveyardTargets(player));
        if (summon == null)
            return;

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, choiceContext))
            return;

        decimal block = source.DynamicVars["Mgc"].BaseValue;
        if (block > 0m)
            await CreatureCmd.GainBlock(player.Creature, block, default, cardPlay);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }

    private static List<BaseMonsterCard> BuildGraveyardTargets(Player player)
    {
        var list = new List<BaseMonsterCard>();
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return list;

        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards))
        {
            if (c is not BaseMonsterCard bm)
                continue;
            if (!bm.Id.Entry.Contains("GRAVEKEEPER", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!bm.CanSummonDuelMonster)
                continue;
            list.Add(bm);
        }

        return list;
    }
}
