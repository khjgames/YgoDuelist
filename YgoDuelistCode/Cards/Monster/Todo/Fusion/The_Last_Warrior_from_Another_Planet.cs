using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class The_Last_Warrior_from_Another_Planet : FusionMonsterCard
{
    /// <summary>Sum of printed DEF added by summon effect; reapplied after full save load (see <c>CardModelFromSerializableMonsterPermanentStatsPatch</c>).</summary>
    [SavedProperty]
    public int SummonAbsorbPrintedDefBonus { get; set; }

    public The_Last_Warrior_from_Another_Planet()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 23,
            baseDef: 23,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Zombyra_the_Dark),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Maryokutai))
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Warrior | YgoCardPackTags.Earth | base.PackTags;

    public override Type[] RelatedCards => new[] { typeof(The_Last_Warrior_from_Another_Planet) };

    /// <summary>MGC uses +1 on upgrade instead of default monster scaling (+2 for base 1).</summary>
    protected override void OnUpgrade()
    {
        int atkBonus = SuppressPrintedAttackUpgradeForEfficiencyTax
            ? 0
            : YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseAtk);
        int defBonus = SuppressPrintedDefenseUpgradeForEfficiencyTax
            ? 0
            : YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseDef);
        DynamicVars.Damage.UpgradeValueBy(atkBonus);
        DynamicVars["Def"].UpgradeValueBy(defBonus);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(defBonus);
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        SyncPermanentExecuteIncreaseVar();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedSummonAbsorbDefBonusToPrintedDefense();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null && CanSummonDuelMonster)
        {
            int tribute = TributeReleaseCount;
            if (tribute > 0)
            {
                if (!TributeSummonPlayPayload.TryTakePendingForManualPlay(choiceContext, this, out var pending) || pending == null
                    || !TributeSummonSelection.TributeSelectionMeetsCost(
                        this,
                        Owner,
                        pending.Pets,
                        pending.MausoleumHpTributes,
                        pending.MausoleumHpLossTotal))
                {
                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);
                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                        await CombatAction(choiceContext, cardPlay);
                    return;
                }

                foreach (Creature pet in pending.Pets)
                    await CreatureCmd.Kill(pet, force: true);

                int hpLoss = pending.MausoleumHpLossTotal;
                if (hpLoss > 0 && Owner.Creature != null)
                {
                    int nextHp = Owner.Creature.CurrentHp - hpLoss;
                    if (nextHp < 0)
                        nextHp = 0;
                    await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
                }
            }

            await DestroyOtherDuelMonstersThenApplySummonBonus();
            await DuelMonsterSummon.TrySummonDuelMonster(Owner, this, choiceContext);
        }

        if (Owner?.Creature != null)
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);

        await CombatAction(choiceContext, cardPlay);
        await OnAfterMonsterPlayResolved(choiceContext, cardPlay);
    }

    private async Task DestroyOtherDuelMonstersThenApplySummonBonus()
    {
        if (Owner?.PlayerCombatState == null)
            return;

        var petsToKill = new List<Creature>();
        foreach (Creature pet in Owner.PlayerCombatState.Pets.ToList())
        {
            if (pet == null || !pet.IsAlive)
                continue;
            BaseMonsterCard? src = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (src == null || ReferenceEquals(src, this))
                continue;
            petsToKill.Add(pet);
        }

        int flatBonus = (int)DynamicVars["Mgc"].BaseValue;
        if (flatBonus > 0)
        {
            ApplyPermanentExecuteAtkDelta(flatBonus);
            ApplyPermanentSummonAbsorbDefDelta(flatBonus);
        }

        foreach (Creature pet in petsToKill)
            await CreatureCmd.Kill(pet, force: true);
    }

    private void ApplyPermanentSummonAbsorbDefDelta(int delta)
    {
        if (delta == 0)
            return;
        AssertMutable();
        SummonAbsorbPrintedDefBonus += delta;
        if (DynamicVars != null)
        {
            DynamicVars["Def"].BaseValue += delta;
            if (DynamicVars.Block != null)
                DynamicVars.Block.BaseValue += delta;
        }

        if (DeckVersion is The_Last_Warrior_from_Another_Planet deck && !ReferenceEquals(deck, this))
        {
            deck.SummonAbsorbPrintedDefBonus += delta;
            if (deck.DynamicVars != null)
            {
                deck.DynamicVars["Def"].BaseValue += delta;
                if (deck.DynamicVars.Block != null)
                    deck.DynamicVars.Block.BaseValue += delta;
            }
        }
    }

    internal void ApplySavedSummonAbsorbDefBonusToPrintedDefense()
    {
        if (SummonAbsorbPrintedDefBonus == 0 || DynamicVars == null)
            return;
        CardModel template = ModelDb.GetById<CardModel>(Id).ToMutable();
        for (int i = 0; i < CurrentUpgradeLevel; i++)
        {
            template.UpgradeInternal();
            template.FinalizeUpgradeInternal();
        }

        decimal baselineDef = template.DynamicVars["Def"].BaseValue;
        DynamicVars["Def"].BaseValue = baselineDef + SummonAbsorbPrintedDefBonus;
        if (DynamicVars.Block != null && template.DynamicVars.Block != null)
            DynamicVars.Block.BaseValue = template.DynamicVars.Block.BaseValue + SummonAbsorbPrintedDefBonus;
    }
}
