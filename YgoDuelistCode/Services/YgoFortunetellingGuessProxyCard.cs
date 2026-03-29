using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public enum YgoFortuneGuessKind
{
    Spell,
    Trap,
    Monster
}

/// <summary>Ephemeral grid card for <see cref="Cards.Trap.Todo.Continuos.Ominous_Fortunetelling"/> type guess; title from <see cref="Patches.PatchesForCards.YgoEnemyIntentProxyCardTitlePatch"/>.</summary>
public sealed class YgoFortunetellingGuessProxyCard : CustomCardModel
{
    public YgoFortuneGuessKind GuessKind { get; }

    public YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind kind)
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: false, autoAdd: false)
    {
        GuessKind = kind;
    }
}
