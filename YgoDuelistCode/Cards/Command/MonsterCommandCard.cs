using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Base for per-monster command option cards. Shares owner and portrait with the source monster card.
/// Needs a parameterless ctor so any reflection-based card scanners don't explode on load.
/// </summary>
public abstract class MonsterCommandCard : CardModel, IYgoCard, ICustomModel
{
    public NormalMonsterCard? SourceMonster { get; private set; }

    /// <summary>
    /// When false, <see cref="Patches.YgoEnergyIconNodePatch"/> hides the energy orb (menu-only options, not paid with energy).
    /// </summary>
    protected internal virtual bool ShowsEnergyCostIcon => true;

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
