using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Royal_Magical_Library : EffectMonsterCard, IMonsterActivatedEffect, IYgoSpellCounterMonster
{
    [SavedProperty]
    public int SpellCounters { get; set; }

    public Royal_Magical_Library()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 0,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spellcaster | YgoCardPackTags.Light | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Royal_Magical_Library) };

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
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ROYAL_MAGICAL_LIBRARY.activated_effect.description";
    public bool IsActivatedEffectAvailable => CurrentSpellCounters >= 3;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player == null || pet == null || !TryConsumeSpellCounters(3))
            return;

        await CardPileCmd.Draw(choiceContext, 1, player);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
