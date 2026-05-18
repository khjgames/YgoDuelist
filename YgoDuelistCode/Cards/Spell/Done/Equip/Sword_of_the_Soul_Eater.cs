using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Sword_of_the_Soul_Eater : BaseEquipSpellCard
{
    private int _tributeBonusAtk;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 6m), new DynamicVar("Mgc2", 3m) };

    public Sword_of_the_Soul_Eater()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CanEquipTo(BaseMonsterCard target) =>
        target is NormalMonsterCard
        && target.GetEffectiveDuelMonsterLevel() <= (int)DynamicVars["Mgc2"].BaseValue;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        int per = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(per + _tributeBonusAtk, 0);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = 8m;
        DynamicVars["Mgc2"].BaseValue = 4m;
    }

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        Player? player = equippedMonster.Owner ?? Owner;
        if (player == null)
            return;

        TaskHelper.RunSafely(TributeOtherNormalsAndApplyBonusAsync(player, equippedMonster));
    }

    private async Task TributeOtherNormalsAndApplyBonusAsync(Player player, BaseMonsterCard equipTarget)
    {
        List<NormalMonsterCard> victims = DuelMonsterFieldRegistry.OrderedFieldMonsters(player)
            .OfType<NormalMonsterCard>()
            .Where(m => !ReferenceEquals(m, equipTarget))
            .ToList();

        if (victims.Count == 0)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        int per = (int)DynamicVars["Mgc"].BaseValue;
        foreach (NormalMonsterCard victim in victims)
        {
            if (victim.Pile?.Type != MonsterPile.CustomType)
                continue;
            await CardPileCmd.Add(victim, gy, CardPilePosition.Top, this, false);
        }

        _tributeBonusAtk = per * victims.Count;
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
    }
}
