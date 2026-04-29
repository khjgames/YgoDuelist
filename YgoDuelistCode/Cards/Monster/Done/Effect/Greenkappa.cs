using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Greenkappa : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt =
        new("cards", "YGODUELIST-GREENKAPPA.flip_destroy_set_select");

    public Greenkappa()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 6,
            baseDef: 9,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Greenkappa) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Greenkappa || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<CardModel> candidates = BuildTargets(cs);
        if (candidates.Count == 0)
            return;

        int cap = (int)DynamicVars["Mgc"].BaseValue;
        int maxPick = Math.Min(cap, candidates.Count);
        if (maxPick <= 0)
            return;

        List<CardModel> destroyed = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipTargetPrompt, 1, maxPick)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildTargets(cs),
            maxResults: maxPick);
        if (destroyed.Count == 0)
            return;

        int energy = 0;
        foreach (CardModel c in destroyed)
        {
            if (!candidates.Contains(c))
                continue;
            if (await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(c, this))
                energy++;
        }

        if (energy > 0 && Owner != null)
            await PlayerCmd.GainEnergy(energy, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 3m;
    }

    private static List<CardModel> BuildTargets(CombatState combatState)
    {
        var candidates = new List<CardModel>();
        YgoFlipSpellTrapFieldEffects.CollectSetSpellTrapsInAllSpellTrapZones(combatState, candidates);
        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(candidates);
    }
}
