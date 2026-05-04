using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Mask_of_Brutality : BaseEquipSpellCard
{
    private const string AtkEnergyImgBbcode =
        "[img]res://YgoDuelist/images/card_frames/attack_monster_energy_icon.png[/img]";

    public Mask_of_Brutality()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    public override bool CardShowsRecklessKeyword => true;

    public override Type[] RelatedCards => new[]
    {
        typeof(Narrow_Pass),
        typeof(Mask_of_Brutality),
        typeof(Mask_of_the_Burdened),
        typeof(Mask_of_Weakness),
    };

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    public override int GetEquipAttackPlayEnergyDiscount(BaseMonsterCard equipped) => 1;

    public override int GetEquipRecklessCombatSelfDamage(BaseMonsterCard equipped) => 1;

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("ATK_Energy", AtkEnergyImgBbcode);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
