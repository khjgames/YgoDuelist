using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster;

public sealed class Muka_Muka : EffectMonsterCard
{

    public Muka_Muka()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 6,
            baseDef: 3,
            baseMgc: 2)
    {
    }

    /// <summary>Use same var types as NormalMonsterCard (DamageVar, BlockVar) so deck/compendium display does not break. Values are base ATK/DEF; actual damage/block in OnPlay is computed.</summary>
    // CalculatedATK/CalculatedDEF come from NormalMonsterCard.CanonicalVars.
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars;

    /// <summary>When not in hand (e.g. deck/compendium) this is 0, so displayed values are base ATK/DEF.</summary>
    private static int GetOtherCardsInHand(CardModel card)
    {
        if (card?.Owner == null)
            return 0;
        if (CombatManager.Instance?.IsInProgress != true)
            return 0;
        var handPile = PileType.Hand.GetPile(card.Owner);
        var hand = handPile.Cards;
        return Math.Max(0, hand.Count(c => c != card));
    }

    private static decimal GetCalculatedAtk(CardModel card)
    {
        if (card is not Muka_Muka m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = (card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc"))
            ? card.DynamicVars["Mgc"].BaseValue
            : (decimal)m.BaseMgc;
        return m.BaseAtk + mgc * others;
    }

    private static decimal GetCalculatedDef(CardModel card)
    {
        if (card is not Muka_Muka m)
            return 0;
        int others = GetOtherCardsInHand(card);
        decimal mgc = (card.DynamicVars != null && card.DynamicVars.ContainsKey("Mgc"))
            ? card.DynamicVars["Mgc"].BaseValue
            : (decimal)m.BaseMgc;
        return m.BaseDef + mgc * others;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(1m);
    }

    /// <summary>
    /// Extra ATK/DEF this card gets from its own effect (hand-based scaling), separate from field auras.
    /// </summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        // Use the same logic as the preview variables but return just the delta over base stats.
        int atk = (int)GetCalculatedAtk(this);
        int def = (int)GetCalculatedDef(this);
        return (atk - BaseAtk, def - BaseDef);
    }
}
