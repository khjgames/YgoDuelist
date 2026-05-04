using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Five_Headed_Dragon : FusionMonsterCard
{
    private static readonly CardKeyword UnyieldingKeyword = (CardKeyword)20062;

    public override int AttackPortionCount => 5;
    /// <summary>YGO: 5 Dragon monsters — requirement-based slots (any Dragon normal/effect that satisfies race).</summary>
    public Five_Headed_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 12,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 50,
            baseDef: 50,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()),
            FusionMaterialSlot.ForRequirement(FusionMaterialRequirements.DragonRaceOnly()))
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fusion | YgoCardPackTags.Dragon | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[] { typeof(Five_Headed_Dragon) };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword kw in base.CanonicalKeywords)
                yield return kw;
            yield return UnyieldingKeyword;
        }
    }

    protected internal override async System.Threading.Tasks.Task OnSummoned(
        Player player,
        PlayerChoiceContext choiceContext,
        Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        decimal stacks = IsUpgradedOrPreviewActive ? 6m : 5m;
        await YgoDuelMonsterProtectionSummon.ApplyUnyieldingAsync(player, duelMonsterPet, stacks);
    }
}
