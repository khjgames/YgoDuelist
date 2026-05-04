using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

/// <summary>Union equip for <see cref="Monster.Done.Effect.Kiryu"/>; only equips to <see cref="Dark_Blade"/>.</summary>
public sealed class Kiryu_Union_Equip : BaseEquipSpellCard, IYgoUnionEquipSpell, IYgoUnionEquipUnequipMonsterOption
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 9m) };

    public Kiryu_Union_Equip()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Spell | YgoCardPackTags.None;

    public override Type[] RelatedCards => new[] { typeof(Kiryu_Union_Equip), typeof(Kiryu) };

    protected override Type[] PreviewReferencedCardTypes => new[]
    {
        typeof(Kiryu),
        typeof(Dark_Blade),
    };

    public override string PortraitPath => ModelDb.Card<Kiryu>().PortraitPath;

    /// <inheritdoc cref="YgoDuelistCard.CustomPortraitPath"/>
    /// <remarks>Defaults would load <c>kiryu_union_equip.png</c>; reuse <see cref="Kiryu"/> art for large/compendium portrait.</remarks>
    public override string CustomPortraitPath =>
        ModelDb.Card<Kiryu>() is YgoDuelistCard src ? src.CustomPortraitPath : base.CustomPortraitPath;

    public override bool CanEquipTo(BaseMonsterCard target) => target is Dark_Blade;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal((int)DynamicVars["Mgc"].BaseValue, 0);

    public override bool GrantsBlightTo(BaseMonsterCard equipped) => true;

    public override bool CardShowsBlightKeyword => true;

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 12m;

    public MonsterCommandCard CreateUnequipMonsterOption(CombatState combatState, Player player) =>
        combatState.CreateCard<Unequip_Kiryu>(player);
}
