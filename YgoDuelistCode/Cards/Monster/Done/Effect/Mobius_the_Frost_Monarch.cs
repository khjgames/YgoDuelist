using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// When Tribute Summoned: optionally destroy up to 2 Spell/Trap Cards in your Spell/Trap Zone; gain
/// <see cref="PlatingPower"/> equal to <see cref="PlatingStacksPerDestroy"/> per card destroyed.
/// </summary>
public sealed class Mobius_the_Frost_Monarch : EffectMonsterCard
{
    private static readonly LocString DestroyPrompt =
        new("cards", "YGODUELIST-MOBIUS_THE_FROST_MONARCH.summon_destroy_spell_trap");

    private bool _hadTributeMaterialsForSummon;

    public Mobius_the_Frost_Monarch()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 24,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Spell | YgoCardPackTags.Trap;
    public override Type[] RelatedCards => new[] { typeof(Mobius_the_Frost_Monarch) };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Plating", 3m);
        }
    }

    protected override void OnBeforeDuelMonsterSummon(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution? tributePending)
    {
        _hadTributeMaterialsForSummon = tributePending != null
            && (tributePending.Pets.Count > 0 || tributePending.MausoleumHpTributes > 0);
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async () =>
            {
                if (!_hadTributeMaterialsForSummon)
                    return;
                if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
                    return;
                if (player.Creature == null)
                    return;

                List<CardModel> candidates = BuildOwnSpellTrapZoneCandidates(player);
                if (candidates.Count == 0)
                    return;

                List<CardModel> pick = await YgoOrderedCardSelection.TryChooseManyAsync(
                    choiceContext,
                    player,
                    new CardSelectorPrefs(DestroyPrompt, 0, 2)
                    {
                        RequireManualConfirmation = true,
                        Cancelable = true
                    },
                    () => BuildOwnSpellTrapZoneCandidates(player),
                    maxResults: 2);

                decimal platingPer = DynamicVars["Plating"].BaseValue;
                foreach (CardModel c in pick)
                {
                    if (!await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(c, this))
                        continue;
                    await PowerCmd.Apply<PlatingPower>(player.Creature, platingPer, player.Creature, this);
                }
            });

    private static List<CardModel> BuildOwnSpellTrapZoneCandidates(Player player)
    {
        var list = new List<CardModel>();
        YgoFlipSpellTrapFieldEffects.CollectOwnerSpellAndTrapCardsInZone(player, list);
        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Plating"].UpgradeValueBy(2m);
    }
}
