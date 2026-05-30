using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Spell counters from spell resolution (HookAfterCardPlayedEffectMonsterPatch).</summary>
public sealed class Magical_Marionette : EffectMonsterCard, IYgoSpellCounterMonster
{
    public override int AttackPortionCount => 2;
    [SavedProperty]
    public int YgoDuelist_SpellCounters { get; set; }

    public Magical_Marionette()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Magical_Marionette) };

    public int CurrentSpellCounters => YgoDuelist_SpellCounters;

    public int MaxSpellCounters => 3;

    public void AddSpellCounter(int amount = 1)
    {
        if (amount <= 0)
            return;
        YgoDuelist_SpellCounters = Math.Min(MaxSpellCounters, YgoDuelist_SpellCounters + amount);
    }

    public bool TryConsumeSpellCounters(int amount)
    {
        if (amount <= 0)
            return true;
        if (YgoDuelist_SpellCounters < amount)
            return false;
        YgoDuelist_SpellCounters -= amount;
        return true;
    }
}
