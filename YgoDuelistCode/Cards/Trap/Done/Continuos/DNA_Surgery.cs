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

public sealed class DNA_Surgery : BaseContinuousTrapCard, IYgoPrePlayCancelableGridSelection
{
    public DuelMonsterRace? DeclaredRace { get; private set; }

    public DNA_Surgery()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        if (player.Creature?.CombatState is not CombatState cs)
            return false;

        List<CardModel> BuildOptions() =>
            Enum.GetValues(typeof(DuelMonsterRace))
                .Cast<DuelMonsterRace>()
                .Where(r => r <= DuelMonsterRace.Zombie)
                .Select(r => (CardModel)YgoTransientSpellOptionCommandCard.CreateWithPortraitPath(
                    cs,
                    player,
                    (int)r,
                    "YGODUELIST-DNA_SURGERY.title",
                    "YGODUELIST-DNA_SURGERY_SELECT.description",
                    this,
                    GetRacePortraitPath(r)))
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
        if (optionId < 0 || optionId > (int)DuelMonsterRace.Zombie)
            return;

        DeclaredRace = (DuelMonsterRace)optionId;
        await DnaFieldOverrideSync.SyncCombatFieldOverridesAsync(Owner);
    }

    protected override void OnUpgrade()
    {
    }

    private static string GetRacePortraitPath(DuelMonsterRace race)
    {
        string file = race switch
        {
            DuelMonsterRace.BeastWarrior => "Beast-Warrior.png",
            DuelMonsterRace.DivineBeast => "Divine-Beast.png",
            DuelMonsterRace.SeaSerpent => "Sea Serpent.png",
            DuelMonsterRace.WingedBeast => "Winged Beast.png",
            _ => $"{race}.png",
        };
        return $"YgoDuelist/images/card_frames/Race/{file}";
    }
}
