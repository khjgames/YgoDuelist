using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Card_7_Completed : BaseEquipSpellCard, IYgoPrePlayCancelableGridSelection
{
    private const int OptionAtk = 0;
    private const int OptionDef = 1;

    private const int PrintedBonus = 7;
    private bool _boostDef;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedBonus) };

    public Card_7_Completed()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Machine;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Machine;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        int bonus = (int)DynamicVars["Mgc"].BaseValue;
        return _boostDef
            ? new StatEffectTotal(0, bonus)
            : new StatEffectTotal(bonus, 0);
    }

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        if (player.Creature?.CombatState is not CombatState cs)
            return false;

        List<CardModel> BuildOptions()
        {
            YgoTransientSpellOptionCommandCard atkOpt = YgoTransientSpellOptionCommandCard.Create(
                cs,
                player,
                OptionAtk,
                "YGODUELIST-CARD_7_COMPLETED_OPT_ATK.title",
                "YGODUELIST-CARD_7_COMPLETED_OPT_ATK.description",
                this,
                PortraitPath);

            YgoTransientSpellOptionCommandCard defOpt = YgoTransientSpellOptionCommandCard.Create(
                cs,
                player,
                OptionDef,
                "YGODUELIST-CARD_7_COMPLETED_OPT_DEF.title",
                "YGODUELIST-CARD_7_COMPLETED_OPT_DEF.description",
                this,
                PortraitPath);

            return new List<CardModel> { atkOpt, defOpt };
        }

        return await YgoPrePlayGridSelection.TryPrepareSingleOptionIdPayloadAsync(
            player,
            sourceCard,
            BuildOptions(),
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: BuildOptions);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 11m;

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        if (YgoPrePlayOptionIdPayload.TryTakePending(this, out int optionId))
            _boostDef = optionId == OptionDef;
    }
}
