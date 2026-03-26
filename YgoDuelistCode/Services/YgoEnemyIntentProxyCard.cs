using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Ephemeral card for choosing an enemy in <see cref="Cards.Trap.Todo.Normal.Dark_Spirit_of_the_Silent"/> second-target grid; title comes from <see cref="Patches.PatchesForCards.YgoEnemyIntentProxyCardTitlePatch"/>.</summary>
public sealed class YgoEnemyIntentProxyCard : CustomCardModel
{
    public Creature? TargetCreature { get; }

    public YgoEnemyIntentProxyCard()
        : this(null)
    {
    }

    public YgoEnemyIntentProxyCard(Creature? targetCreature)
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: false, autoAdd: false)
    {
        TargetCreature = targetCreature;
    }
}
