using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Scroll_of_Bewitchment : BaseEquipSpellCard, IYgoPrePlayCancelableGridSelection
{
    private const int PrintedBonus = 1;

    public DuelMonsterAttribute? SelectedAttribute { get; private set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedBonus) };

    public Scroll_of_Bewitchment()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, (int)DynamicVars["Mgc"].BaseValue);

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
                    "YGODUELIST-SCROLL_OF_BEWITCHMENT.title",
                    "YGODUELIST-SCROLL_OF_BEWITCHMENT_SELECT.description",
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

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 3m;
        EnergyCost.UpgradeBy(-1);
    }

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        if (YgoPrePlayOptionIdPayload.TryTakePending(this, out int optionId)
            && optionId >= 0 && optionId <= (int)DuelMonsterAttribute.Divine)
        {
            SelectedAttribute = (DuelMonsterAttribute)optionId;
        }

        Player? player = equippedMonster.Owner ?? Owner;
        if (player == null || !SelectedAttribute.HasValue)
            return;

        TaskHelper.RunSafely(
            YgoEquipPetPowerAttach.ApplyAttributeOverrideAsync(player, equippedMonster, SelectedAttribute.Value, this));
    }

    protected internal override void OnAfterDetachedFromFieldMonster(BaseMonsterCard equippedMonster)
    {
        TaskHelper.RunSafely(YgoEquipPetPowerAttach.RemoveAttributeOverrideAsync(equippedMonster));
    }

    private static string GetAttributePortraitPath(DuelMonsterAttribute attribute) =>
        $"YgoDuelist/images/card_frames/Attribute/{attribute.ToString().ToLowerInvariant()}.png";
}
