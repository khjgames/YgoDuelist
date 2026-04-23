using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;

public sealed class The_Sanctuary_in_the_Sky : BaseFieldSpellCard, IYgoOwnerTurnStartSpellTrapZoneEffect
{
    private const string AnnualKey = "SANCTUARY_MERCURY_DRAW";

    public The_Sanctuary_in_the_Sky()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Light;

    public YgoOwnerTurnStartSpellTrapDispatchPhase OwnerTurnStartSpellTrapDispatchPhase =>
        YgoOwnerTurnStartSpellTrapDispatchPhase.BeforeOwnerFieldPetHooks;

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    public bool IsOwnerTurnStartSpellTrapZoneEffectActive() => !FaceDown;

    public async Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsOwnerTurnStartSpellTrapZoneEffectActive())
            return;
        if (player.PlayerCombatState == null)
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard m
                && m.ParticipatesInSanctuaryMercuryDraw)
            {
                await CardPileCmd.Draw(choiceContext, 1, player);
                break;
            }
        }
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
