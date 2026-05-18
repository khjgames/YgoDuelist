using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Dragonic_Attack : BaseEquipSpellCard
{
    private const int PrintedBonus = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedBonus) };

    public Dragonic_Attack()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Warrior | YgoCardPackTags.Dragon;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Warrior;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, (int)DynamicVars["Mgc"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 7m;

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        Player? player = equippedMonster.Owner ?? Owner;
        if (player == null)
            return;

        TaskHelper.RunSafely(
            YgoEquipPetPowerAttach.ApplyRaceOverrideAsync(player, equippedMonster, DuelMonsterRace.Dragon, this));
    }

    protected internal override void OnAfterDetachedFromFieldMonster(BaseMonsterCard equippedMonster)
    {
        TaskHelper.RunSafely(YgoEquipPetPowerAttach.RemoveRaceOverrideAsync(equippedMonster));
    }
}
