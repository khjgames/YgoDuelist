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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: Special Summon 1 "La Jinn the Mystical Genie of the Lamp" from your hand.</summary>
public sealed class Ancient_Lamp : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString PickPrompt =
        new("cards", "YGODUELIST-ANCIENT_LAMP.activated_effect.selection");

    public Ancient_Lamp()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 9,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Wind | YgoCardPackTags.Spellcaster;

    public override Type[] RelatedCards => new[] { typeof(Ancient_Lamp), typeof(La_Jinn_the_Mystical_Genie_of_the_Lamp) };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ANCIENT_LAMP.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && BuildLaJinnHandCandidates(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        Player? player = source.Owner;
        if (player?.Creature == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<La_Jinn_the_Mystical_Genie_of_the_Lamp> pool = BuildLaJinnHandCandidates(player);
        if (pool.Count == 0)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null)
            return;

        CardModel? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => pool.Cast<CardModel>().ToList());
        if (picked is not La_Jinn_the_Mystical_Genie_of_the_Lamp pick || !pool.Contains(pick))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null || !hand.Cards.Contains(pick))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, pick, choiceContext);
    }

    private static List<La_Jinn_the_Mystical_Genie_of_the_Lamp> BuildLaJinnHandCandidates(Player player)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return new List<La_Jinn_the_Mystical_Genie_of_the_Lamp>();

        return YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(hand.Cards)
            .OfType<La_Jinn_the_Mystical_Genie_of_the_Lamp>()
            .ToList();
    }
}
