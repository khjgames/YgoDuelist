using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

/// <summary>Equip only to a Warrior. Grants Magic Protection and Monster Protection; +Mgc ATK.</summary>
public sealed class Fusion_Sword_Murasame_Blade : BaseEquipSpellCard
{
    private static readonly CardKeyword MagicProtectionKeyword = (CardKeyword)20063;
    private static readonly CardKeyword MonsterProtectionKeyword = (CardKeyword)20064;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 8m) };

    public Fusion_Sword_Murasame_Blade()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Fusion_Sword_Murasame_Blade) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Append(MagicProtectionKeyword).Append(MonsterProtectionKeyword);

    public override bool CanEquipTo(BaseMonsterCard target) =>
        target.DuelMonsterRace == DuelMonsterRace.Warrior;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        Player? player = equippedMonster.Owner;
        if (player == null)
            return;
        TaskHelper.RunSafely(ApplyProtectionsToEquippedAsync(player, equippedMonster));
    }

    private static async System.Threading.Tasks.Task ApplyProtectionsToEquippedAsync(Player player, BaseMonsterCard equipped)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, equipped);
        if (pet == null || !pet.IsAlive)
            return;
        await YgoDuelMonsterProtectionSummon.ApplyMagicProtectionAsync(player, pet);
        await YgoDuelMonsterProtectionSummon.ApplyMonsterProtectionAsync(player, pet);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 11m;
}
