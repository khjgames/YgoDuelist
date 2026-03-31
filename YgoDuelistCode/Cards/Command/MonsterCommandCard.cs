using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Base for per-monster command option cards. Shares owner and portrait with the source monster card.
/// Needs a parameterless ctor so any reflection-based card scanners don't explode on load.
/// </summary>
public abstract class MonsterCommandCard : CardModel, IYgoCard, ICustomModel
{
    private const int AttributeKeywordBase = 10000;
    private const int RaceKeywordBase = 20000;

    public NormalMonsterCard? SourceMonster { get; private set; }

    /// <summary>
    /// When false, <see cref="Patches.YgoEnergyIconNodePatch"/> hides the energy orb (menu-only options, not paid with energy).
    /// Ignored when <see cref="CustomCommandEnergyTexturePath"/> is set.
    /// </summary>
    protected internal virtual bool ShowsEnergyCostIcon => true;

    /// <summary>
    /// Optional mod resource path (e.g. <c>YgoDuelist/images/card_frames/foo.png</c>) for the energy orb texture.
    /// When non-null, the orb stays visible and uses this texture instead of the default command styling.
    /// <see cref="Patches.YgoMonsterCommandEnergyCostVisualPatch"/> also applies this texture to the hand unplayable
    /// energy overlay and can blank the <c>0</c> cost label.
    /// </summary>
    protected internal virtual string? CustomCommandEnergyTexturePath => null;

    /// <summary>
    /// When non-null and <see cref="CustomCommandEnergyTexturePath"/> is empty, <see cref="Patches.YgoEnergyIconNodePatch"/> uses this
    /// prefix with <see cref="MegaCrit.Sts2.Core.Helpers.EnergyIconHelper.GetPath"/> (e.g. <c>"silent"</c> for spell-like commands).
    /// </summary>
    protected internal virtual string? CommandEnergyIconPrefix => null;

    // Parameterless ctor for reflection / scanners – never used at runtime for real commands.
    protected MonsterCommandCard()
        : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    // Runtime ctor used when we explicitly construct command cards from a monster source.
    protected MonsterCommandCard(NormalMonsterCard source, int cost, CardType type, TargetType target)
        : base(cost, type, CardRarity.Event, target)
    {
        InitializeSource(source);
    }

    public void InitializeSource(NormalMonsterCard source)
    {
        SourceMonster = source;
        CardModelEnergyCache.Invalidate(this);
    }

    public YgoCardType YgoCardType => YgoCardType.Spell;

    public DuelMonsterRace DuelMonsterRace =>
        SourceMonster?.DuelMonsterRace ?? DuelMonsterRace.Warrior;

    // Default to the source monster's portrait if available; otherwise use the generic card back so command cards always have art.
    public override string PortraitPath =>
        !string.IsNullOrEmpty(SourceMonster?.PortraitPath)
            ? SourceMonster.PortraitPath
            : "card.png".CardImagePath();

    // Use the dedicated command card pool so these menu-only commands render in UIs without falling back to MockCardPool.
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<YgoCommandCardPool>();

    /// <summary>
    /// Base <see cref="CardModel.Pool"/> only resolves if this card's id is listed in a pool's <c>AllCards</c>.
    /// Menu-only commands that are not listed in <see cref="YgoCommandCardPool.AllCards"/> still need a pool; tie <see cref="Pool"/>
    /// to <see cref="VisualCardPool"/> so UI code never hits <see cref="InvalidProgramException"/> ("not in any card pool").
    /// </summary>
    public override CardPoolModel Pool => VisualCardPool;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            var source = SourceMonster;
            if (source == null)
                return base.CanonicalKeywords;

            var result = new List<CardKeyword>(4)
            {
                (CardKeyword)(AttributeKeywordBase + (int)source.DuelMonsterAttribute),
                (CardKeyword)(RaceKeywordBase + (int)source.DuelMonsterRace)
            };

            int level = source.GetEffectiveDuelMonsterLevel();
            if (level >= 7)
                result.Add((CardKeyword)20035); // Tribute Summon (2)
            else if (level >= 5)
                result.Add((CardKeyword)20034); // Tribute Summon (1)

            return result;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var source = SourceMonster;
            if (source == null)
                return base.ExtraHoverTips;

            var tips = new List<IHoverTip>(4);
            foreach (var kw in CanonicalKeywords)
                tips.Add(HoverTipFactory.FromKeyword(kw));

            return tips;
        }
    }

    /// <summary>
    /// Helper identical in spirit to BaseSpellCard.SendThisSpellToGraveyard,
    /// but routes this command card into the YgoCardOptionPile so BaseLib's
    /// CustomPile hooks can control visibility/layout.
    /// </summary>
    public async Task SendThisCommandToYgoOptionPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { this },
            optionPile,
            CardPilePosition.Top,
            this,
            false);
    }
}
