using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster;

/// <summary>
/// Enraged Muka Muka – stronger variant of Muka Muka.
/// Same effect: ATK & DEF scale with the number of other cards in hand.
/// </summary>
public sealed class Enraged_Muka_Muka : EffectMonsterCard
{
    public Enraged_Muka_Muka()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 6,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Rock,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    /// <summary>Vars computed from base stats; CalculatedATK/CalculatedDEF come from NormalMonsterCard.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars;

    private static int GetOtherCardsInHand(CardModel card)
    {
        if (card?.Owner == null)
            return 0;
        if (CombatManager.Instance?.IsInProgress != true)
            return 0;
        var handPile = PileType.Hand.GetPile(card.Owner);
        var hand = handPile.Cards;
        return hand.Count(c => c != card);
    }

    private static decimal GetPrintedAtk(CardModel card, Enraged_Muka_Muka m) =>
        card.DynamicVars?.Damage != null ? card.DynamicVars.Damage.BaseValue : m.BaseAtk;

    private static decimal GetPrintedDef(CardModel card, Enraged_Muka_Muka m)
    {
        if (card.DynamicVars != null && card.DynamicVars.ContainsKey("Def"))
            return card.DynamicVars["Def"].BaseValue;
        if (card.DynamicVars?.Block != null)
            return card.DynamicVars.Block.BaseValue;
        return m.BaseDef;
    }

    private static decimal GetCalculatedAtk(CardModel card)
    {
        if (card is not Enraged_Muka_Muka m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = (card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc"))
            ? card.DynamicVars["Mgc"].BaseValue
            : (decimal)m.BaseMgc;
        return GetPrintedAtk(card, m) + mgc * others;
    }

    private static decimal GetCalculatedDef(CardModel card)
    {
        if (card is not Enraged_Muka_Muka m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = (card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc"))
            ? card.DynamicVars["Mgc"].BaseValue
            : (decimal)m.BaseMgc;
        return GetPrintedDef(card, m) + mgc * others;
    }

    /// <summary>
    /// Extra ATK/DEF this card gets from its own effect (hand-based scaling), separate from field auras.
    /// </summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        int printedAtk = (int)GetPrintedAtk(this, this);
        int printedDef = (int)GetPrintedDef(this, this);
        int atk = (int)GetCalculatedAtk(this);
        int def = (int)GetCalculatedDef(this);
        return (atk - printedAtk, def - printedDef);
    }
}
