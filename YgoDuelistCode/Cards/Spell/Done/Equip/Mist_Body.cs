using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

/// <summary>Equip only to a Beast-Warrior. Grants Unyielding to the equipped duel monster.</summary>
public sealed class Mist_Body : BaseEquipSpellCard
{
    private static readonly CardKeyword UnyieldingKeyword = (CardKeyword)20062;
    private const int PrintedStacks = 3;
    private const int PrintedStacksUpgraded = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", PrintedStacks) };

    public Mist_Body()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Mist_Body) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Append(UnyieldingKeyword);

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        Player? player = equippedMonster.Owner;
        if (player == null)
            return;
        TaskHelper.RunSafely(ApplyUnyieldingToEquippedAsync(player, equippedMonster));
    }

    private static async System.Threading.Tasks.Task ApplyUnyieldingToEquippedAsync(Player player, BaseMonsterCard equipped)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, equipped);
        if (pet == null || !pet.IsAlive)
            return;
        decimal stacks = equipped.IsUpgradedOrPreviewActive ? 5m : 3m;
        await YgoDuelMonsterProtectionSummon.ApplyUnyieldingAsync(player, pet, stacks);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = PrintedStacksUpgraded;
    }
}
