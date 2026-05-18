using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Horn_of_Light : BaseEquipSpellCard, IYgoEquipSentFromSpellTrapZoneToGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-HORN_OF_LIGHT.activate");
    private const string PayOptionTitleKey = "YGODUELIST-HORN_OF_LIGHT.pay_option.title";
    private const string PayOptionDescriptionKey = "YGODUELIST-HORN_OF_LIGHT.pay_option.description";

    private const int PrintedDef = 7;
    private const int HpCost = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedDef) };

    public Horn_of_Light()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(0, (int)DynamicVars["Mgc"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 10m;

    public Task OnSentFromSpellTrapZoneToGraveyardAsync(Player owner) =>
        YgoEquipGraveyardFromZoneEffects.TryOptionalPayHpReturnToDeckTopAsync(
            owner,
            this,
            ActivatePrompt,
            PayOptionTitleKey,
            PayOptionDescriptionKey,
            HpCost);
}
