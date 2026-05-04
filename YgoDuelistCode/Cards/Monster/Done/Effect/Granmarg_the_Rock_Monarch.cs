using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When Tribute Summoned: destroy 1 Set card in your Spell/Trap or Monster Zone; gain 1 Conduit (and 1 Energy when upgraded).</summary>
public sealed class Granmarg_the_Rock_Monarch : EffectMonsterCard
{
    private static readonly LocString DestroyPrompt =
        new("cards", "YGODUELIST-GRANMARG_THE_ROCK_MONARCH.summon_destroy_select");

    private bool _hadTributeMaterialsForSummon;

    public Granmarg_the_Rock_Monarch()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 24,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Granmarg_the_Rock_Monarch) };

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
                if (player.Creature?.CombatState == null)
                    return;

                List<CardModel> candidates = BuildSetCardCandidates(player);
                if (candidates.Count == 0)
                    return;

                CardModel? pick = await YgoOrderedCardSelection.TryChooseSingleAsync(
                    choiceContext,
                    player,
                    new CardSelectorPrefs(DestroyPrompt, 1, 1)
                    {
                        RequireManualConfirmation = true,
                        Cancelable = true,
                    },
                    () => BuildSetCardCandidates(player));
                if (pick == null || !candidates.Contains(pick))
                    return;

                if (pick is BaseMonsterCard bm && bm.FaceDown && DuelMonsterFieldRegistry.ContainsFieldMonster(player, bm))
                {
                    Creature? pet = YgoMpCombatOrder.FirstPetWhere(
                        player.PlayerCombatState!,
                        p => DuelMonsterFieldRegistry.HasSourceCard(p, bm));
                    if (pet != null)
                        await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                            YgoDestructionSourceKind.MonsterEffect,
                            pet);
                }
                else if (await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(pick, this))
                {
                }

                await PlayerCmd.GainStars(1, player);
                if (IsUpgradedOrPreviewActive)
                    await PlayerCmd.GainEnergy(1, player);
            });

    private static List<CardModel> BuildSetCardCandidates(Player player)
    {
        var list = new List<CardModel>();
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone != null)
        {
            foreach (CardModel c in zone.Cards)
            {
                switch (c)
                {
                    case BaseSpellCard s when s.FaceDown:
                        list.Add(s);
                        break;
                    case BaseTrapCard t when t.FaceDown:
                        list.Add(t);
                        break;
                }
            }
        }

        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (m == null || !m.FaceDown)
                continue;
            list.Add(m);
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list);
    }
}
