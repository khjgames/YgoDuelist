using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Character;

namespace YgoDuelist.YgoDuelistCode.Services;

public enum YgoFortuneGuessKind
{
    Spell,
    Trap,
    Monster
}

/// <summary>Ephemeral grid card for <see cref="Cards.Trap.Todo.Continuos.Ominous_Fortunetelling"/> type guess; title from <see cref="Patches.PatchesForCards.YgoEnemyIntentProxyCardTitlePatch"/>.</summary>
[Pool(typeof(YgoDuelistCardPool))]
public sealed class YgoFortunetellingGuessProxyCard : CustomCardModel
{
    public YgoFortuneGuessKind GuessKind { get; }

    /// <summary>ModelDb requires a parameterless ctor; Fortunetelling uses <see cref="YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind)"/> for real picks.</summary>
    public YgoFortunetellingGuessProxyCard()
        : this(YgoFortuneGuessKind.Spell)
    {
    }

    public YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind kind)
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: false, autoAdd: false)
    {
        GuessKind = kind;
    }
}
