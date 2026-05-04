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

/// <summary>Union equip for <see cref="Monster.Done.Effect.Pitch_Dark_Dragon"/>; only equips to <see cref="Dark_Blade"/>.</summary>
public sealed class Pitch_Dark_Dragon_Union_Equip : BaseEquipSpellCard, IYgoUnionEquipSpell, IYgoUnionEquipUnequipMonsterOption
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 4m) };

    public Pitch_Dark_Dragon_Union_Equip()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Spell | YgoCardPackTags.None;

    public override Type[] RelatedCards => new[] { typeof(Pitch_Dark_Dragon_Union_Equip), typeof(Pitch_Dark_Dragon) };

    protected override Type[] PreviewReferencedCardTypes => new[]
    {
        typeof(Pitch_Dark_Dragon),
        typeof(Dark_Blade),
    };

    public override string PortraitPath => ModelDb.Card<Pitch_Dark_Dragon>().PortraitPath;

    /// <inheritdoc cref="YgoDuelistCard.CustomPortraitPath"/>
    /// <remarks>Defaults would load <c>pitch_dark_dragon_union_equip.png</c>; reuse <see cref="Pitch_Dark_Dragon"/> art for large/compendium portrait.</remarks>
    public override string CustomPortraitPath =>
        ModelDb.Card<Pitch_Dark_Dragon>() is YgoDuelistCard src ? src.CustomPortraitPath : base.CustomPortraitPath;

    public override bool CanEquipTo(BaseMonsterCard target) => target is Dark_Blade;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, mgc);
    }

    public override bool GrantsSplinterTo(BaseMonsterCard equipped) => true;

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 6m;

    public MonsterCommandCard CreateUnequipMonsterOption(CombatState combatState, Player player) =>
        combatState.CreateCard<Unequip_Pitch_Dark_Dragon>(player);
}
