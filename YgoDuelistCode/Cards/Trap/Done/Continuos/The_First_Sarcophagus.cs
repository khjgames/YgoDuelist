using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

/// <summary>
/// Continuous Trap: each of your turn starts while face-up, place the next Sarcophagus spell from your hand, draw pile, or discard pile.
/// When all three are face-up on the field, destroy them and Special Summon <see cref="Spirit_of_the_Pharaoh"/>.
/// If any Sarcophagus piece leaves the field, the others are destroyed (<see cref="YgoSarcophagusChain"/>).
/// </summary>
public sealed class The_First_Sarcophagus : BaseContinuousTrapCard, IYgoOwnerTurnStartSpellTrapZoneEffect
{
    public The_First_Sarcophagus()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Trap | YgoCardPackTags.Zombie | YgoCardPackTags.Spell;

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
            typeof(The_Second_Sarcophagus),
            typeof(The_Third_Sarcophagus),
            typeof(Spirit_of_the_Pharaoh));

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public bool IsOwnerTurnStartSpellTrapZoneEffectActive() => !FaceDown && Pile?.Type == SpellTrapZonePile.CustomType;

    public async Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsOwnerTurnStartSpellTrapZoneEffectActive() || Owner == null || !ReferenceEquals(Owner, player))
            return;

        await YgoSarcophagusChain.ResolveTurnStartAsync(choiceContext, player, this);
    }
}
