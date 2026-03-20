using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Base type for all YgoDuelist monster cards. Right-click toggles Attack (attack position) vs Skill (defense position).
/// </summary>
public abstract class AbstractMonsterCard : YgoDuelistCard, IYgoCard
{
    // We don't have real STS CardKeyword entries for Yu-Gi-Oh! attributes/races,
    // so we map our enums into "virtual" CardKeyword ids by using stable numeric values.
    // HoverTipFactory.FromKeyword then looks these up in localization table `card_keywords`.
    private const int AttributeKeywordBase = 10000;
    private const int RaceKeywordBase = 20000;

    private static CardKeyword AttributeToKeyword(DuelMonsterAttribute attribute)
        => (CardKeyword)(AttributeKeywordBase + (int)attribute);

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    private static CardKeyword SpecialSummonKeyword => (CardKeyword)20033;
    private static CardKeyword FaceDownKeyword => (CardKeyword)10012;
    private static CardKeyword TributeSummon1Keyword => (CardKeyword)20034;
    private static CardKeyword TributeSummon2Keyword => (CardKeyword)20035;
    private static CardKeyword FusionMonsterKeyword => (CardKeyword)20036;
    private static CardKeyword RitualMonsterKeyword => (CardKeyword)20037;

    public abstract YgoCardType YgoCardType { get; }
    public bool FaceDown { get; set; } = false;
    public bool WillSet { get; set; } = true;

    /// <summary>True = attack position (Attack card), false = defense position (Skill card). Toggle via right-click in hand.</summary>
    private bool _displayAsAttack;

    public override CardType Type => _displayAsAttack ? CardType.Attack : CardType.Skill;
    public override TargetType TargetType => _displayAsAttack ? TargetType.AnyEnemy : TargetType.Self;

    public new LocString Description => GetDescriptionLocString();

    protected AbstractMonsterCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        _displayAsAttack = (type == CardType.Attack);
    }

    /// <summary>
    /// Sets whether this monster starts in attack position (Attack card) or defense position (Skill card).
    /// Called by derived classes once their stats (e.g. base ATK/DEF) are known.
    /// </summary>
    protected void SetDisplayAttackSkill(bool displayAsAttack)
    {
        _displayAsAttack = displayAsAttack;
        if (Type == CardType.Attack && FaceDown == true){
            FaceDown = false; 
        }
        else if (Type == CardType.Skill && FaceDown == false && WillSet == true){
            FaceDown = true;
        }
        UpdateFaceDownKeywordFromBool();
    }

    /// <summary>
    /// Sets attack vs defense position when the player uses <see cref="Command.Command_Attack"/> or <see cref="Command.Command_Defend"/>.
    /// Persists after the command resolves (same rules as <see cref="SetDisplayAttackSkill"/> for face-down).
    /// </summary>
    public void SetBattlePositionFromDuelCommand(bool attackPosition)
    {
        SetDisplayAttackSkill(attackPosition);
    }

    /// <summary>
    /// Monster command menu only: switches battle position.
    /// Defense → attack: face-up and <see cref="WillSet"/> = false (no automatic set).
    /// Attack → defense: position only; <see cref="FaceDown"/> and <see cref="WillSet"/> unchanged.
    /// </summary>
    public void ApplyBattlePositionChangeFromCommandMenu()
    {
        if (Type == CardType.Skill)
        {
            _displayAsAttack = true;
            FaceDown = false;
            WillSet = false;
        }
        else
        {
            _displayAsAttack = false;
        }

        UpdateFaceDownKeywordFromBool();
    }

    /// <summary>Swaps between Attack and Skill (attack position / defense position). Called by right-click in hand.</summary>
    public void ToggleAttackSkill()
    {
        _displayAsAttack = !_displayAsAttack;
        if (Type == CardType.Attack && FaceDown == true){
            FaceDown = false; 
        }
        else if (Type == CardType.Skill && FaceDown == false && WillSet == true){
            FaceDown = true;
        }
        UpdateFaceDownKeywordFromBool();
    }

    /// <summary>Level (star count) 1-9+ for summon HP. Override per card.</summary>
    public virtual int DuelMonsterLevel => 4;

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK). Override per card.</summary>
    public virtual DuelMonsterAttribute DuelMonsterAttribute => DuelMonsterAttribute.Earth;

    /// <summary>Duel monster race / type icon. <see cref="BaseMonsterCard"/> supplies the real value.</summary>
    public virtual DuelMonsterRace DuelMonsterRace => DuelMonsterRace.Warrior;

    /// <summary>If true, playing this monster card can summon a duel monster in a zone (max 5 per player).</summary>
    public virtual bool CanSummonDuelMonster => true;

    /// <summary>
    /// Monsters released for a normal tribute summon: 0 unless level 5+ non-ritual non-fusion (1 for level 5–6, 2 for 7+).
    /// Uses current <see cref="DuelMonsterLevel"/> (updates when level changes).
    /// </summary>
    public int TributeReleaseCount => ComputeTributeReleaseCount();

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

    private bool IsRitualOrFusionMonster =>
        YgoCardType == YgoCardType.RitualMonster || YgoCardType == YgoCardType.FusionMonster;

    private IEnumerable<CardKeyword> GetFusionAndRitualKeywords()
    {
        if (YgoCardType == YgoCardType.FusionMonster)
            yield return FusionMonsterKeyword;
        else if (YgoCardType == YgoCardType.RitualMonster)
            yield return RitualMonsterKeyword;
    }

    private int ComputeTributeReleaseCount()
    {
        if (IsRitualOrFusionMonster)
            return 0;

        int level = DuelMonsterLevel;
        if (level < 5)
            return 0;
        if (level <= 6)
            return 1;
        return 2;
    }

    private IEnumerable<CardKeyword> GetSummonKeywordsByMonsterLevel()
    {
        int n = ComputeTributeReleaseCount();
        if (n == 1)
            yield return TributeSummon1Keyword;
        else if (n >= 2)
            yield return TributeSummon2Keyword;
    }

    private IEnumerable<CardKeyword> GetFaceDownKeywordsFromBool()
    {
        if (FaceDown)
            yield return FaceDownKeyword;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            List<CardKeyword> keywords = new List<CardKeyword>(capacity: 2);
            keywords.Add(AttributeToKeyword(DuelMonsterAttribute));
            keywords.Add(RaceToKeyword(DuelMonsterRace));
            keywords.AddRange(GetFusionAndRitualKeywords());
            keywords.AddRange(GetFaceDownKeywordsFromBool());
            foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
                keywords.Add(kw);
            return keywords;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> tips = new List<IHoverTip>(capacity: 2);
            tips.Add(HoverTipFactory.FromKeyword(AttributeToKeyword(DuelMonsterAttribute)));
            tips.Add(HoverTipFactory.FromKeyword(RaceToKeyword(DuelMonsterRace)));
            foreach (CardKeyword kw in GetFusionAndRitualKeywords())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetFaceDownKeywordsFromBool())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            return tips;
        }
    }

    public void UpdateFaceDownKeywordFromBool()
    {
        if (!IsMutable)
            return;

        _ = Keywords;
        RemoveKeyword(FaceDownKeyword);
        foreach (CardKeyword kw in GetFaceDownKeywordsFromBool())
            AddKeyword(kw);

        DuelMonsterStancePowerSync.RequestSyncIfSummoned(this);
    }

    /// <summary>
    /// Call this after changing <see cref="DuelMonsterLevel"/> so the card's keyword hover tooltips
    /// stay correct. Removes and re-applies 20033/20034/20035 based on current level.
    /// </summary>
    public void RefreshSummonKeywordsForMonsterLevel()
    {
        // Force init of the backing HashSet so RemoveKeyword/AddKeyword won't NRE.
        _ = Keywords;

        RemoveKeyword(SpecialSummonKeyword);
        RemoveKeyword(TributeSummon1Keyword);
        RemoveKeyword(TributeSummon2Keyword);

        foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
        {
            AddKeyword(kw);
        }
    }
}
