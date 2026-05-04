using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Union-Effect: equip to <see cref="Normal.Dark_Blade"/> via <see cref="Union_Equip"/>.</summary>
public sealed class Pitch_Dark_Dragon : EffectMonsterCard, IUnionEffectMonster, IMonsterOptionCommandProvider
{
    public Type UnionEquipSpellType => typeof(Pitch_Dark_Dragon_Union_Equip);

    public Pitch_Dark_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Dragon | YgoCardPackTags.Normal;
    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferenceOrder.LeadingThenMerged(
            GetType(),
            new[] { typeof(Pitch_Dark_Dragon_Union_Equip), typeof(Dark_Blade) });

    public IEnumerable<MonsterCommandCard> BuildExtraMonsterOptionCommands(
        CombatState combatState,
        Player player,
        Creature pet)
    {
        _ = pet;
        var cmd = combatState.CreateCard<Union_Equip>(player);
        cmd.InitializeSource(this);
        return new MonsterCommandCard[] { cmd };
    }
}
