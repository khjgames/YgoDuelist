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
/// Command-style shell for extra smart tips where you want a <b>different</b> title/body than the real card id (via
/// <c>preview_effect</c> loc keys + Harmony), while borrowing the host’s portrait. Do <b>not</b> use this to preview a
/// whole existing card type — use <c>HoverTipFactory.FromCard(ModelDb.Card&lt;T&gt;())</c> so frame, level, attributes, and keywords render.
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
    /// Hosted <see cref="PreviewEffect"/> smart tip: extended effect copy under <c>{host.Id.Entry}.preview_effect.*</c>
    /// (or fallback title / <see cref="CardModel.GetDescriptionForPile"/>). Not for full-card previews of another type.
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
