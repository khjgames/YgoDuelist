using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Spirit_Reaper : EffectMonsterCard
{
    private static readonly CardKeyword UnyieldingKeyword = (CardKeyword)20062;
    private const int UnyieldingStacks = 3;
    private const int UnyieldingStacksUpgraded = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Uyd", UnyieldingStacks) });

    public Spirit_Reaper()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 3,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] RelatedCards => new[] { typeof(Spirit_Reaper) };

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
        await YgoDuelMonsterProtectionSummon.ApplyUnyieldingAsync(
            player, duelMonsterPet, DynamicVars["Uyd"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Uyd"].BaseValue = UnyieldingStacksUpgraded;
    }
}
