using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Mask_of_the_Burdened : BaseEquipSpellCard
{
    private const string DefEnergyImgBbcode =
        "[img]res://YgoDuelist/images/card_frames/defense_monster_energy_icon.png[/img]";

    public Mask_of_the_Burdened()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

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

    public override int GetEquipDefensePlayEnergyDiscount(BaseMonsterCard equipped) => 1;

    public override int GetEquipRecklessCombatSelfDamage(BaseMonsterCard equipped) => 1;

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("DEF_Energy", DefEnergyImgBbcode);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
