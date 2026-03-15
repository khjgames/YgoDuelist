using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Base type for all YgoDuelist monster cards. Right-click toggles Attack (attack position) vs Skill (defense position).
/// </summary>
public abstract class AbstractMonsterCard : YgoDuelistCard, IYgoCard
{
    public abstract YgoCardType YgoCardType { get; }

    /// <summary>True = attack position (Attack card), false = defense position (Skill card). Toggle via right-click in hand.</summary>
    private bool _displayAsAttack;

    public override CardType Type => _displayAsAttack ? CardType.Attack : CardType.Skill;

    public new LocString Description => GetDescriptionLocString();

    protected AbstractMonsterCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        _displayAsAttack = (type == CardType.Attack);
    }

    /// <summary>Swaps between Attack and Skill (attack position / defense position). Called by right-click in hand.</summary>
    public void ToggleAttackSkill()
    {
        _displayAsAttack = !_displayAsAttack;
    }

    /// <summary>Level (star count) 1-9+ for summon HP. Override per card.</summary>
    public virtual int DuelMonsterLevel => 4;

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK). Override per card.</summary>
    public virtual DuelMonsterAttribute DuelMonsterAttribute => DuelMonsterAttribute.Earth;

    /// <summary>If true, playing this monster card can summon a duel monster in a zone (max 5 per player).</summary>
    public virtual bool CanSummonDuelMonster => true;

    /// <summary>Returns the correct description LocString for attack vs skill form. Used by description patch.</summary>
    public LocString GetDescriptionLocString()
    {
        string suffix = _displayAsAttack ? ".description" : ".description_skill";
        if (IsInHand())
            suffix += "_combat";
        return new LocString("cards", Id.Entry + suffix);
    }

    private bool IsInHand()
    {
        // Default must be compendium-safe: only switch to combat text when we're in an actual combat hand pile.
        if (CombatManager.Instance?.IsInProgress != true)
            return false;
        return Pile?.Type == PileType.Hand;
    }
}
