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
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Skilled_Dark_Magician : EffectMonsterCard, IMonsterActivatedEffect, IYgoSpellCounterMonster
{
    private static readonly LocString SelectPrompt = new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    [SavedProperty]
    public int SpellCounters { get; set; }

    public Skilled_Dark_Magician()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 19,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spellcaster | YgoCardPackTags.Dark | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Skilled_Dark_Magician), typeof(Dark_Magician) };

    public int CurrentSpellCounters => SpellCounters;
    public int MaxSpellCounters => 3;

    public void AddSpellCounter(int amount = 1)
    {
        if (amount <= 0)
            return;
        SpellCounters = Math.Min(MaxSpellCounters, SpellCounters + amount);
    }

    public bool TryConsumeSpellCounters(int amount)
    {
        if (amount <= 0)
            return true;
        if (SpellCounters < amount)
            return false;
        SpellCounters -= amount;
        return true;
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-SKILLED_DARK_MAGICIAN.activated_effect.description";
    public bool IsActivatedEffectAvailable => Owner != null && CurrentSpellCounters >= 3 && BuildSummonTargets(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player == null || pet == null || !TryConsumeSpellCounters(3))
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 1))
            return;

        if (BuildSummonTargets(player).Count == 0)
            return;

        List<Dark_Magician> targets = BuildSummonTargets(player);
        Dark_Magician? summon = targets.Count == 1
            ? targets[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(SelectPrompt, 1, 1) { Cancelable = true },
                () => BuildSummonTargets(player));
        if (summon == null)
            return;

        await CreatureCmd.Kill(pet, force: true);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy != null)
            await CardPileCmd.Add(new[] { source }, gy, CardPilePosition.Top, source, false);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, choiceContext))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<Dark_Magician> BuildSummonTargets(Player player)
    {
        return YgoPlayerPiles.OrderedSummonableMonstersFromPiles<Dark_Magician>(
            player,
            YgoPlayerPiles.Hand,
            YgoPlayerPiles.Draw,
            YgoPlayerPiles.Discard,
            GraveyardRelic.GetGraveyardPile);
    }
}
