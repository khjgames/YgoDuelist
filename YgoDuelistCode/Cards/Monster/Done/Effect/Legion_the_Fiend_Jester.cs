using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Field effect: <see cref="LegionFiendJesterSpellcasterConduit"/> waives conduit (stars) on normal/tribute Spellcaster hand summons while Legions are on the field.
/// </summary>
public sealed class Legion_the_Fiend_Jester : EffectMonsterCard, IYgoLegionFiendJesterFieldMonster
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    public Legion_the_Fiend_Jester()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Spellcaster | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
    [
        typeof(Legion_the_Fiend_Jester),
        typeof(Double_Summon),
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        //typeof(Toon_Dark_Magician_Girl),
        typeof(Dark_Magic_Attack),
    ];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("conduitIcon", ConduitImgBbcode);
    }
}
