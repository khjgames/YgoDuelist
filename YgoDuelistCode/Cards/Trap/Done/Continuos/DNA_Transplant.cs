using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

public sealed class DNA_Transplant : BaseContinuousTrapCard, IYgoPrePlayCancelableGridSelection
{
    public DuelMonsterAttribute? DeclaredAttribute { get; private set; }

    public DNA_Transplant()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        if (player.Creature?.CombatState is not CombatState cs)
            return false;

        List<CardModel> BuildOptions() =>
            Enum.GetValues(typeof(DuelMonsterAttribute))
                .Cast<DuelMonsterAttribute>()
                .Select(a => (CardModel)YgoTransientSpellOptionCommandCard.CreateWithPortraitPath(
                    cs,
                    player,
                    (int)a,
                    "YGODUELIST-DNA_TRANSPLANT.title",
                    "YGODUELIST-DNA_TRANSPLANT_SELECT.description",
                    this,
                    GetAttributePortraitPath(a)))
                .ToList();

        return await YgoPrePlayGridSelection.TryPrepareSingleOptionIdPayloadAsync(
            player,
            sourceCard,
            BuildOptions(),
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: BuildOptions);
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;
        if (!YgoPrePlayOptionIdPayload.TryTakePending(this, out int optionId))
            return;
        if (optionId < 0 || optionId > (int)DuelMonsterAttribute.Divine)
            return;

        DeclaredAttribute = (DuelMonsterAttribute)optionId;
        await DnaFieldOverrideSync.SyncCombatFieldOverridesAsync(Owner);
    }

    protected override void OnUpgrade()
    {
    }

    private static string GetAttributePortraitPath(DuelMonsterAttribute attribute)
    {
        return $"YgoDuelist/images/card_frames/Attribute/{attribute.ToString().ToLowerInvariant()}.png";
    }
}
