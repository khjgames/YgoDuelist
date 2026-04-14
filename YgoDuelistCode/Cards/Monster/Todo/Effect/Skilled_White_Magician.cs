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
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Skilled_White_Magician : EffectMonsterCard, IMonsterActivatedEffect, IYgoSpellCounterMonster
{
    private static readonly LocString SelectPrompt = new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    [SavedProperty]
    public int SpellCounters { get; set; }

    public Skilled_White_Magician()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 19,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spellcaster | YgoCardPackTags.Light | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Skilled_White_Magician), typeof(Buster_Blader) };

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
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-SKILLED_WHITE_MAGICIAN.activated_effect.description";
    public bool IsActivatedEffectAvailable => Owner != null && CurrentSpellCounters >= 3 && BuildTargets(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player == null || pet == null || !TryConsumeSpellCounters(3))
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 1))
            return;

        List<Buster_Blader> targets = BuildTargets(player);
        if (targets.Count == 0)
            return;

        Buster_Blader summon = targets[0];
        if (targets.Count > 1)
        {
            IEnumerable<CardModel> pick;
            try
            {
                pick = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    targets.Cast<CardModel>().ToList(),
                    player,
                    new CardSelectorPrefs(SelectPrompt, 1, 1) { Cancelable = true });
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Buster_Blader? chosen = pick.OfType<Buster_Blader>().FirstOrDefault();
            if (chosen == null)
                return;
            summon = chosen;
        }

        await CreatureCmd.Kill(pet, force: true);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (gy != null)
            await CardPileCmd.Add(new[] { source }, gy, CardPilePosition.Top, source, false);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, choiceContext))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<Buster_Blader> BuildTargets(Player player)
    {
        var list = new List<Buster_Blader>();
        Append(PileType.Hand.GetPile(player), list);
        Append(PileType.Draw.GetPile(player), list);
        Append(PileType.Discard.GetPile(player), list);
        Append(GraveyardRelic.GetGraveyardPile(player), list);
        return list;
    }

    private static void Append(CardPile? pile, List<Buster_Blader> outList)
    {
        if (pile == null)
            return;
        foreach (CardModel c in pile.Cards)
        {
            if (c is Buster_Blader bb)
                outList.Add(bb);
        }
    }
}
