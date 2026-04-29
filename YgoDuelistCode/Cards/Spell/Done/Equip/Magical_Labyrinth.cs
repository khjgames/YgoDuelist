using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

/// <summary>Equip only to <see cref="Labyrinth_Wall"/>. Tribute command is <see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Special_Summon_Wall_Shadow"/>.</summary>
public sealed class Magical_Labyrinth : BaseEquipSpellCard
{
    public Magical_Labyrinth()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Spell;

    public override Type[] RelatedCards =>
        new[] { typeof(Magical_Labyrinth), typeof(Labyrinth_Wall), typeof(Wall_Shadow) };

    public override bool CanEquipTo(BaseMonsterCard target) => target is Labyrinth_Wall;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public static bool IsFaceUpEquippedTo(Labyrinth_Wall wall) =>
        YgoEquipSpellRegistry.GetEquipsForMonster(wall)
            .Any(e => e is Magical_Labyrinth ml && ml.Pile?.Type == SpellTrapZonePile.CustomType && !ml.FaceDown);
}
