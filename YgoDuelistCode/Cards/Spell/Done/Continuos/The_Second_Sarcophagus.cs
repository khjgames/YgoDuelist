using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;

/// <summary>
/// Continuous Spell placed only by <see cref="The_First_Sarcophagus"/>. <see cref="IYgoBrickCard"/>.
/// </summary>
public sealed class The_Second_Sarcophagus : BaseContinuousSpellCard, IYgoBrickCard
{
    public The_Second_Sarcophagus()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Zombie | YgoCardPackTags.Trap;

    public override Type[] RelatedCards =>
        new[]
        {
            typeof(The_First_Sarcophagus),
            typeof(The_Second_Sarcophagus),
            typeof(The_Third_Sarcophagus),
            typeof(Spirit_of_the_Pharaoh),
        };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(
            GetType(),
            typeof(The_First_Sarcophagus),
            typeof(The_Third_Sarcophagus),
            typeof(Spirit_of_the_Pharaoh));

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override bool IsPlayable =>
        base.IsPlayable
        && (Pile?.Type == SpellTrapZonePile.CustomType
            || YgoFirstSarcophagusPlacementGate.IsActive);

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
