using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Armed_Ninja : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt =
        new("cards", "YGODUELIST-ARMED_NINJA.flip_destroy_spell_select");

    public Armed_Ninja()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 3,
            baseDef: 3,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Armed_Ninja) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<NextSpellDiscountPower>();
            yield return HoverTipFactory.FromPower<NextSpellDiscountPowerPlus>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Armed_Ninja || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<BaseSpellCard> BuildTargets()
        {
            var candidates = new List<BaseSpellCard>();
            YgoFlipSpellTrapFieldEffects.CollectSpellsInAllSpellTrapZones(cs, candidates);
            return candidates;
        }

        List<BaseSpellCard> candidates = BuildTargets();
        if (candidates.Count == 0)
            return;

        BaseSpellCard? victim = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipTargetPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            BuildTargets);
        if (victim == null)
            return;

        bool destroyed = await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(victim, this);
        if (!destroyed || Owner.Creature == null)
            return;

        if (IsUpgraded)
            await PowerCmd.Apply<NextSpellDiscountPowerPlus>(Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<NextSpellDiscountPower>(Owner.Creature, 1m, Owner.Creature, this);
        YgoSpellTrapDiscountEnergyRefresh.ForPlayer(Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
