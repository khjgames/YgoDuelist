using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Special Summon only via <see cref="Great_Dezard"/>. Magic Protection.
/// Activate Effect: face-down Defense Position. FLIP: Special Summon 1 Zombie from your Graveyard.
/// </summary>
public sealed class Fushioh_Richie : EffectMonsterCard, IMonsterActivatedEffect, IMonsterFlipEffect
{
    private static readonly CardKeyword MagicProtectionKeyword = (CardKeyword)20063;

    private static readonly LocString FlipZombiePrompt =
        new("cards", "YGODUELIST-FUSHIOH_RICHIE.flip_summon_zombie");

    public Fushioh_Richie()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 26,
            baseDef: 29,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] RelatedCards => new[] { typeof(Fushioh_Richie), typeof(Great_Dezard) };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        Owner != null && YgoFushiohRichieSummonGate.AllowsSpecialSummon(Owner, this);

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword kw in base.CanonicalKeywords)
                yield return kw;
            yield return MagicProtectionKeyword;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<MagicProtectionKeywordPower>();
        }
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        await YgoDuelMonsterProtectionSummon.ApplyMagicProtectionAsync(player, duelMonsterPet);
    }

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-FUSHIOH_RICHIE.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && !FaceDown
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        if (source is not Fushioh_Richie || Owner == null)
            return;

        Player player = Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, player);
        if (pet == null)
            return;

        FaceDown = true;
        await ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(choiceContext, player, attackPosition: false);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Fushioh_Richie || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> zombies = BuildZombieGraveyardSummonCandidates(player);
        if (zombies.Count == 0)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(FlipZombiePrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildZombieGraveyardSummonCandidates(player));
        if (chosen == null)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext);
    }

    private static List<BaseMonsterCard> BuildZombieGraveyardSummonCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m.DuelMonsterRace == DuelMonsterRace.Zombie
                && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate)
                && !m.BlocksSpecialDuelMonsterSummon)
            .ToList();
}
