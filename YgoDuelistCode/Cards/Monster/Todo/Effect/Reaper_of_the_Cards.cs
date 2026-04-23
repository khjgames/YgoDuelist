using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Reaper_of_the_Cards : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt =
        new("cards", "YGODUELIST-REAPER_OF_THE_CARDS.flip_destroy_trap_select");

    public Reaper_of_the_Cards()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 19,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Reaper_of_the_Cards) };

    private bool ShowNextTrapDiscountPlus => IsUpgradedOrPreviewActive;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            if (ShowNextTrapDiscountPlus)
                yield return HoverTipFactory.FromPower<NextTrapDiscountPowerPlus>();
            else
                yield return HoverTipFactory.FromPower<NextTrapDiscountPower>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Reaper_of_the_Cards || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<BaseTrapCard> candidates = BuildTargets(cs);
        if (candidates.Count == 0)
            return;

        BaseTrapCard? victim = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipTargetPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildTargets(cs));
        if (victim == null)
            return;

        bool destroyed = await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(victim, this);
        if (!destroyed || Owner.Creature == null)
            return;

        if (IsUpgraded)
            await PowerCmd.Apply<NextTrapDiscountPowerPlus>(Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<NextTrapDiscountPower>(Owner.Creature, 1m, Owner.Creature, this);
        YgoSpellTrapDiscountEnergyRefresh.ForPlayer(Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    private static List<BaseTrapCard> BuildTargets(CombatState combatState)
    {
        var candidates = new List<BaseTrapCard>();
        YgoFlipSpellTrapFieldEffects.CollectTrapsInAllSpellTrapZones(combatState, candidates);
        return candidates;
    }
}
