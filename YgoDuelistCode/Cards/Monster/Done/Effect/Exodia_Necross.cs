using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Exodia_Necross : EffectMonsterCard, IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset
{
    private static readonly CardKeyword UnyieldingKeyword = (CardKeyword)20062;
    private static readonly CardKeyword MagicProtectionKeyword = (CardKeyword)20063;
    private const int UnyieldingStacks = 3;
    private const int UnyieldingStacksUpgraded = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Uyd", UnyieldingStacks) });

    public Exodia_Necross()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 0,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.WinCon;

    public override Type[] RelatedCards => new[] { typeof(Exodia_Necross), typeof(Contract_with_Exodia) };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword kw in base.CanonicalKeywords)
                yield return kw;
            yield return UnyieldingKeyword;
            yield return MagicProtectionKeyword;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<ExodiaNecrossAtkPower>();
            yield return HoverTipFactory.FromPower<UnyieldingPower>();
            yield return HoverTipFactory.FromPower<MagicProtectionKeywordPower>();
        }
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        await YgoDuelMonsterProtectionSummon.ApplyUnyieldingAsync(player, duelMonsterPet, DynamicVars["Uyd"].BaseValue);
        await YgoDuelMonsterProtectionSummon.ApplyMagicProtectionAsync(player, duelMonsterPet);
    }

    public async Task ApplyTurnStartAtkGrowthAsync(Player player, Creature pet)
    {
        ExodiaNecrossAtkPower? p = pet.GetPower<ExodiaNecrossAtkPower>();
        decimal mgc = DynamicVars["Mgc"].BaseValue;
        if (p == null)
            await PowerCmd.Apply<ExodiaNecrossAtkPower>(pet, mgc, player.Creature, null);
        else
            await PowerCmd.ModifyAmount(p, mgc, player.Creature, null);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 6m;
        DynamicVars["Uyd"].BaseValue = UnyieldingStacksUpgraded;
    }
}
