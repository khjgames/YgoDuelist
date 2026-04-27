using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Mausoleum of the Emperor synthetic HP row in tribute selection grids (<see cref="IYgoMausoleumHpTributeOption"/>).
/// </summary>
public sealed class Mausoleum_Lose_HP : MonsterCommandCard, IYgoMausoleumHpTributeOption
{
    public const int BaseTributeHpLoss = 10;
    public const int UpgradedTributeHpLoss = 6;

    public Mausoleum_Lose_HP()
    {
    }

    public Mausoleum_Lose_HP(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    public override bool UseAlternateUpgradedDescription => true;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public int TributeHpLoss => IsUpgraded ? UpgradedTributeHpLoss : BaseTributeHpLoss;

    /// <summary>0..2 when created as a tribute-grid HP row; used for deterministic ordering across MP peers.</summary>
    public int MausoleumGridSlot { get; internal set; }

    public override string PortraitPath => "mausoleum_of_the_emperor.png".CardImagePath();

    internal override bool ShouldPatchTitleToCardsTitleUpgradedLoc(CardModel self) =>
        self.IsUpgraded || self.UpgradePreviewType != CardUpgradePreviewType.None;
}
