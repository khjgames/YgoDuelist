using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Single class for choose-a-card UI options: set <see cref="TitleCardsLocKey"/>, <see cref="DescriptionCardsLocKey"/>,
/// <see cref="OptionId"/>, optional <see cref="DynamicVarSource"/> for <c>{Mgc}</c> etc., and portrait via <see cref="Configure"/>.
/// </summary>
public sealed class YgoTransientSpellOptionCommandCard : MonsterCommandCard
{
    private string? _portraitPathOverride;

    public YgoTransientSpellOptionCommandCard()
    {
    }

    /// <summary>Caller-defined discriminator (e.g. 0 = heal, 1 = blight).</summary>
    public int OptionId { get; private set; }

    /// <summary>Full <c>cards.json</c> key for title, e.g. <c>YGODUELIST-POISON_OF_THE_OLD_MAN_OPT_HEAL.title</c>.</summary>
    public string TitleCardsLocKey { get; private set; } = "";

    /// <summary>Full <c>cards.json</c> key for description.</summary>
    public string DescriptionCardsLocKey { get; private set; } = "";

    /// <summary>Vars for placeholders are read from this card when set (usually the spell being played).</summary>
    public CardModel? DynamicVarSource { get; private set; }

    public bool IsConfigured => !string.IsNullOrEmpty(TitleCardsLocKey);

    public void Configure(
        int optionId,
        string titleCardsLocKey,
        string descriptionCardsLocKey,
        CardModel? dynamicVarSource,
        string portraitPngFileName)
    {
        OptionId = optionId;
        TitleCardsLocKey = titleCardsLocKey;
        DescriptionCardsLocKey = descriptionCardsLocKey;
        DynamicVarSource = dynamicVarSource;
        _portraitPathOverride = portraitPngFileName.CardImagePath();
    }

    public static YgoTransientSpellOptionCommandCard Create(
        CombatState combatState,
        Player player,
        int optionId,
        string titleCardsLocKey,
        string descriptionCardsLocKey,
        CardModel? dynamicVarSource,
        string portraitPngFileName)
    {
        var card = (YgoTransientSpellOptionCommandCard)combatState.CreateCard<YgoTransientSpellOptionCommandCard>(player);
        card.Configure(optionId, titleCardsLocKey, descriptionCardsLocKey, dynamicVarSource, portraitPngFileName);
        return card;
    }

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath =>
        !string.IsNullOrEmpty(_portraitPathOverride) ? _portraitPathOverride : base.PortraitPath;
}
