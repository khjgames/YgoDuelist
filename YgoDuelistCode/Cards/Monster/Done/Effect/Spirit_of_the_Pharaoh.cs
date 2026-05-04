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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Special Summon only via <see cref="The_First_Sarcophagus"/> trio.
/// On Special Summon: you may Special Summon up to 4 Zombie-Type monsters with level at most {Mgc} from your Graveyard.
/// </summary>
public sealed class Spirit_of_the_Pharaoh : EffectMonsterCard
{
    private static readonly LocString ZombiePrompt =
        new("cards", "YGODUELIST-SPIRIT_OF_THE_PHARAOH.summon_zombies");

    public Spirit_of_the_Pharaoh()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 25,
            baseDef: 20,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Zombie | YgoCardPackTags.Bundled;

    public override Type[] BundledCards =>
        new[]
        {
            typeof(The_First_Sarcophagus),
            typeof(The_Second_Sarcophagus),
            typeof(The_Third_Sarcophagus),
        };

    public override Type[] RelatedCards =>
        new[]
        {
            typeof(Spirit_of_the_Pharaoh),
            typeof(The_First_Sarcophagus),
            typeof(The_Second_Sarcophagus),
            typeof(The_Third_Sarcophagus),
        };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoSpiritOfThePharaohSummonGate.IsActive;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);

        int cap = (int)DynamicVars["Mgc"].BaseValue;
        List<BaseMonsterCard> candidates = BuildZombieGraveyardCandidates(player, cap);
        if (candidates.Count == 0)
            return;

        List<BaseMonsterCard> picked = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(ZombiePrompt, 0, 4)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildZombieGraveyardCandidates(player, cap),
            maxResults: 4);

        foreach (BaseMonsterCard z in picked)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                break;
            if (!YgoPlayerPiles.GraveyardContains(player, z))
                continue;
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, z, choiceContext);
        }
    }

    private static List<BaseMonsterCard> BuildZombieGraveyardCandidates(Player player, int maxLevel) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m is not Spirit_of_the_Pharaoh
                && m.DuelMonsterRace == DuelMonsterRace.Zombie
                && m.GetEffectiveDuelMonsterLevel() <= maxLevel
                && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate)
                && !m.BlocksSpecialDuelMonsterSummon)
            .ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
