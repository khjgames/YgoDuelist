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

public sealed class Malevolent_Nuzzler : BaseEquipSpellCard, IYgoEquipSentFromSpellTrapZoneToGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-MALEVOLENT_NUZZLER.activate");
    private const string PayOptionTitleKey = "YGODUELIST-MALEVOLENT_NUZZLER.pay_option.title";
    private const string PayOptionDescriptionKey = "YGODUELIST-MALEVOLENT_NUZZLER.pay_option.description";

    private const int PrintedAtk = 7;
    private const int HpCost = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtk) };

    public Malevolent_Nuzzler()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Fiend;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Fiend;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

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
