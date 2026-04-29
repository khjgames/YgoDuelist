using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Dragon_Seeker : EffectMonsterCard, IMonsterFlipEffect
{
    /// <summary>Printed DEF added by effect resolution; reapplied after full save load.</summary>
    [SavedProperty]
    public int DragonSeekerPrintedDefBonus { get; set; }

    public Dragon_Seeker()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 21,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Dragon_Seeker) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            () => TryResolveDragonSeekerDestroyAndGrowAsync(player, choiceContext));

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Dragon_Seeker || Owner == null)
            return;
        await TryResolveDragonSeekerDestroyAndGrowAsync(Owner, choiceContext);
    }

    private async Task TryResolveDragonSeekerDestroyAndGrowAsync(Player player, PlayerChoiceContext choiceContext)
    {
        if (player.PlayerCombatState == null)
            return;

        List<Creature> faceUpDragonPets = YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState)
            .Where(p => p != null && p.IsAlive)
            .Where(p =>
            {
                BaseMonsterCard? src = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p);
                return src != null && !src.FaceDown && src.DuelMonsterRace == DuelMonsterRace.Dragon;
            })
            .ToList();

        if (faceUpDragonPets.Count == 0)
            return;

        Creature chosen = YgoDeterministicRng
            .StableOrder(faceUpDragonPets, p => p.CombatId)
            .First();
        if (!chosen.IsAlive)
            return;

        await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
            YgoDestructionSourceKind.MonsterEffect,
            chosen);

        int bonus = (int)DynamicVars["Mgc"].BaseValue;
        if (bonus <= 0)
            return;
        ApplyPermanentExecuteAtkDelta(bonus);
        ApplyPermanentDragonSeekerDefDelta(bonus);
    }

    private void ApplyPermanentDragonSeekerDefDelta(int delta)
    {
        if (delta == 0)
            return;
        AssertMutable();
        DragonSeekerPrintedDefBonus += delta;
        if (DynamicVars != null)
        {
            DynamicVars["Def"].BaseValue += delta;
            if (DynamicVars.Block != null)
                DynamicVars.Block.BaseValue += delta;
        }

        if (DeckVersion is Dragon_Seeker deck && !ReferenceEquals(deck, this))
        {
            deck.DragonSeekerPrintedDefBonus += delta;
            if (deck.DynamicVars != null)
            {
                deck.DynamicVars["Def"].BaseValue += delta;
                if (deck.DynamicVars.Block != null)
                    deck.DynamicVars.Block.BaseValue += delta;
            }
        }
    }

    public override void ApplyPostDeserializePrintedStatBonuses()
    {
        base.ApplyPostDeserializePrintedStatBonuses();
        ApplySavedDragonSeekerDefBonusToPrintedDefense();
    }

    internal void ApplySavedDragonSeekerDefBonusToPrintedDefense()
    {
        if (DragonSeekerPrintedDefBonus == 0 || DynamicVars == null)
            return;
        CardModel template = ModelDb.GetById<CardModel>(Id).ToMutable();
        for (int i = 0; i < CurrentUpgradeLevel; i++)
        {
            template.UpgradeInternal();
            template.FinalizeUpgradeInternal();
        }

        decimal baselineDef = template.DynamicVars["Def"].BaseValue;
        DynamicVars["Def"].BaseValue = baselineDef + DragonSeekerPrintedDefBonus;
        if (DynamicVars.Block != null && template.DynamicVars.Block != null)
            DynamicVars.Block.BaseValue = template.DynamicVars.Block.BaseValue + DragonSeekerPrintedDefBonus;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedDragonSeekerDefBonusToPrintedDefense();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
