using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Display-only card shell for <c>HoverTipFactory.FromCard</c> smart tips; bind <see cref="PreviewHost"/> to mirror a card's portrait.
/// Matches <see cref="Heads"/> styling; optional <see cref="PreviewHost"/> reuses that card's portrait for the preview face.
/// </summary>
public sealed class PreviewEffect : MonsterCommandCard
{
    private const string DefaultPortraitPath = "YgoDuelist/images/card_frames/PreviewEffect.png";

    public YgoDuelistCard? PreviewHost { get; private set; }

    public PreviewEffect()
    {
    }

    public PreviewEffect(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    /// <summary>
    /// Bind a host card so <see cref="PortraitPath"/> can mirror its art (e.g. before wrapping in <c>HoverTipFactory.FromCard</c>).
    /// </summary>
    public void SetPreviewHost(YgoDuelistCard? host)
    {
        PreviewHost = host;
        CardModelEnergyCache.Invalidate(this);
    }

    /// <summary>
    /// Smart card hover tip using this type’s frame; portrait from <see cref="PreviewHost"/>; title/body from
    /// <c>cards.json</c> <c>{host.Id.Entry}.preview_effect.title</c> / <c>.preview_effect.description</c>
    /// (game <c>CardModel</c> id stays <c>PREVIEW_EFFECT</c>, so hosted strings are applied by Harmony in
    /// <c>PreviewEffectHostedHoverLocPatch</c>).
    /// </summary>
    public static IHoverTip CreateHostedSmartTip(YgoDuelistCard host)
    {
        var pe = (PreviewEffect)ModelDb.Card<PreviewEffect>().ToMutable();
        pe.SetPreviewHost(host);
        return HoverTipFactory.FromCard(pe);
    }

    protected override HashSet<CardTag> CanonicalTags => new() { YgoDuelistPreviews.PreviewEffect };

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath
    {
        get
        {
            if (PreviewHost is { } h && !string.IsNullOrEmpty(h.PortraitPath))
                return h.PortraitPath;
            if (!string.IsNullOrEmpty(SourceMonster?.PortraitPath))
                return SourceMonster.PortraitPath;
            return DefaultPortraitPath;
        }
    }
}
