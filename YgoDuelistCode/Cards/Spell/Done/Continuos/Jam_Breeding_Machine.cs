using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using DuelMonsterSummon = YgoDuelist.YgoDuelistCode.Services.DuelMonsterSummon;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;

public sealed class Jam_Breeding_Machine : BaseContinuousSpellCard, IYgoOwnerTurnStartSpellTrapZoneEffect
{
    public Jam_Breeding_Machine()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Jam_Breeding_Machine), typeof(Slime_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Slime_Token));

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public bool IsOwnerTurnStartSpellTrapZoneEffectActive() => !FaceDown;

    public async Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsOwnerTurnStartSpellTrapZoneEffectActive())
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(player, "JAM_BREEDING_MACHINE"))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;
        if (!ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.Aqua))
            return;

        await YgoTokenSummon.TrySpecialSummonTokenAsync<Slime_Token>(player, choiceContext, defensePosition: false);
    }
}
