using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Sword_of_Dragon_S_Soul : BaseEquipSpellCard
{
    private const int PrintedAtk = 6;
    private const int PrintedPerDragon = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", PrintedAtk), new DynamicVar("Mgc2", PrintedPerDragon) };

    public Sword_of_Dragon_S_Soul()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Warrior | YgoCardPackTags.Dragon;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Warrior;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        Player? owner = equipped.Owner ?? Owner;
        if (owner == null)
            return new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

        int dragons = CountDragonMonstersInGraveyard(owner);
        int baseAtk = (int)DynamicVars["Mgc"].BaseValue;
        int perDragon = (int)DynamicVars["Mgc2"].BaseValue;
        return new StatEffectTotal(baseAtk + dragons * perDragon, 0);
    }

    private static int CountDragonMonstersInGraveyard(Player owner) =>
        YgoPlayerPiles.GraveyardCards(owner)
            .OfType<BaseMonsterCard>()
            .Count(m => m.DuelMonsterRace == DuelMonsterRace.Dragon);

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 8m;
        DynamicVars["Mgc2"].BaseValue = 2m;
    }
}
