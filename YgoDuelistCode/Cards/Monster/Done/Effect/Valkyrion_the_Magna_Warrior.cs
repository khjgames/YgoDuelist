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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: Tribute 1 Alpha, 1 Beta, and 1 Gamma from your hand and/or field to Special Summon.
/// Activate Effect: Tribute this card, then Special Summon 1 of each Magnet Warrior from your Graveyard.
/// </summary>
public sealed class Valkyrion_the_Magna_Warrior : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString PickAlphaPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_alpha_graveyard");

    private static readonly LocString PickBetaPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_beta_graveyard");

    private static readonly LocString PickGammaPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_gamma_graveyard");

    private static readonly LocString PickAlphaMaterialPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_alpha_material");

    private static readonly LocString PickBetaMaterialPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_beta_material");

    private static readonly LocString PickGammaMaterialPrompt =
        new("cards", "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.pick_gamma_material");

    public Valkyrion_the_Magna_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 35,
            baseDef: 38,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
        new[]
        {
            typeof(Valkyrion_the_Magna_Warrior),
            typeof(Alpha_the_Magnet_Warrior),
            typeof(Beta_the_Magnet_Warrior),
            typeof(Gamma_the_Magnet_Warrior),
        };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanMeetMagnaHandSpecialSummon(Owner));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanMeetMagnaHandSpecialSummon(player))
                return;

            HashSet<BaseMonsterCard> used = new();
            BaseMonsterCard? alphaMat = await TryPickMagnetMaterialAsync(
                choiceContext,
                player,
                PickAlphaMaterialPrompt,
                () => BuildMagnetMaterialPool<Alpha_the_Magnet_Warrior>(player, used));
            if (alphaMat == null)
                return;
            used.Add(alphaMat);

            BaseMonsterCard? betaMat = await TryPickMagnetMaterialAsync(
                choiceContext,
                player,
                PickBetaMaterialPrompt,
                () => BuildMagnetMaterialPool<Beta_the_Magnet_Warrior>(player, used));
            if (betaMat == null)
                return;
            used.Add(betaMat);

            BaseMonsterCard? gammaMat = await TryPickMagnetMaterialAsync(
                choiceContext,
                player,
                PickGammaMaterialPrompt,
                () => BuildMagnetMaterialPool<Gamma_the_Magnet_Warrior>(player, used));
            if (gammaMat == null)
                return;

            if (!ValidateMaterialStillPresent(player, alphaMat)
                || !ValidateMaterialStillPresent(player, betaMat)
                || !ValidateMaterialStillPresent(player, gammaMat))
                return;

            CardPile? gy = YgoPlayerPiles.Graveyard(player);
            if (gy == null)
                return;

            foreach (BaseMonsterCard m in new[] { alphaMat, betaMat, gammaMat })
            {
                Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
                if (pet != null)
                {
                    if (!pet.IsAlive)
                        return;
                    await CreatureCmd.Kill(pet, force: true);
                }
                else if (YgoPlayerPiles.Hand(player)?.Cards.Contains(m) == true)
                    await CardPileCmd.Add(new[] { m }, gy, CardPilePosition.Top, m, false);
                else
                    return;
            }

            if (Pile?.Type != PileType.Hand)
                return;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-VALKYRION_THE_MAGNA_WARRIOR.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && DuelMonsterSummon.CountLiveDuelMonsters(Owner) <= 3
        && HasMagnetTrioInGraveyard(Owner)
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        if (source is not Valkyrion_the_Magna_Warrior || Owner?.Creature == null)
            return;

        Player player = Owner;
        if (!HasMagnetTrioInGraveyard(player))
            return;
        if (DuelMonsterSummon.CountLiveDuelMonsters(player) > 3)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, player);
        if (selfPet == null)
            return;

        BaseMonsterCard? alphaGy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickAlphaPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildGraveyardMagnetPool<Alpha_the_Magnet_Warrior>(player));
        if (alphaGy == null)
            return;

        BaseMonsterCard? betaGy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickBetaPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildGraveyardMagnetPool<Beta_the_Magnet_Warrior>(player));
        if (betaGy == null)
            return;

        BaseMonsterCard? gammaGy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickGammaPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildGraveyardMagnetPool<Gamma_the_Magnet_Warrior>(player));
        if (gammaGy == null)
            return;

        if (ReferenceEquals(alphaGy, betaGy)
            || ReferenceEquals(alphaGy, gammaGy)
            || ReferenceEquals(betaGy, gammaGy))
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, alphaGy)
            || !YgoPlayerPiles.GraveyardContains(player, betaGy)
            || !YgoPlayerPiles.GraveyardContains(player, gammaGy))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
        await CreatureCmd.Kill(selfPet, force: true);

        foreach (BaseMonsterCard target in new[] { alphaGy, betaGy, gammaGy })
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                break;
            if (!YgoPlayerPiles.GraveyardContains(player, target))
                continue;
            if (!target.CanSummonDuelMonster && !target.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate)
                continue;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, target, choiceContext);
        }
    }

    private static async Task<BaseMonsterCard?> TryPickMagnetMaterialAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        LocString prompt,
        Func<List<BaseMonsterCard>> rebuild)
    {
        if (rebuild().Count == 0)
            return null;

        return await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(prompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            rebuild);
    }

    private static bool ValidateMaterialStillPresent(Player player, BaseMonsterCard m)
    {
        if (TributeSummonSelection.ResolvePetForFieldCard(player, m) is { IsAlive: true })
            return true;
        return YgoPlayerPiles.Hand(player)?.Cards.Contains(m) == true;
    }

    private static List<BaseMonsterCard> BuildMagnetMaterialPool<TMag>(Player player, HashSet<BaseMonsterCard> used)
        where TMag : BaseMonsterCard
    {
        var list = new List<BaseMonsterCard>();
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand != null)
        {
            list.AddRange(
                YgoMpCombatOrder
                    .CardsSnapshotOrderedForMp(hand.Cards)
                    .OfType<TMag>()
                    .Where(m => !used.Contains(m)));
        }

        if (player.PlayerCombatState != null)
        {
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (!pet.IsAlive)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not TMag m)
                    continue;
                if (used.Contains(m))
                    continue;
                list.Add(m);
            }
        }

        return list;
    }

    private static bool CanMeetMagnaHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> alphas = BuildMagnetMaterialPool<Alpha_the_Magnet_Warrior>(player, new HashSet<BaseMonsterCard>());
        List<BaseMonsterCard> betas = BuildMagnetMaterialPool<Beta_the_Magnet_Warrior>(player, new HashSet<BaseMonsterCard>());
        List<BaseMonsterCard> gammas = BuildMagnetMaterialPool<Gamma_the_Magnet_Warrior>(player, new HashSet<BaseMonsterCard>());
        foreach (BaseMonsterCard a in alphas)
        {
            foreach (BaseMonsterCard b in betas)
            {
                foreach (BaseMonsterCard g in gammas)
                {
                    if (ReferenceEquals(a, b) || ReferenceEquals(a, g) || ReferenceEquals(b, g))
                        continue;
                    int fieldTributes = CountFieldMaterialsAmong(player, a, b, g);
                    if (DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, fieldTributes))
                        return true;
                }
            }
        }

        return false;
    }

    private static int CountFieldMaterialsAmong(Player player, params BaseMonsterCard[] mats)
    {
        int n = 0;
        foreach (BaseMonsterCard m in mats)
        {
            if (TributeSummonSelection.ResolvePetForFieldCard(player, m) != null)
                n++;
        }

        return n;
    }

    private static bool HasMagnetTrioInGraveyard(Player player) =>
        BuildGraveyardMagnetPool<Alpha_the_Magnet_Warrior>(player).Count > 0
        && BuildGraveyardMagnetPool<Beta_the_Magnet_Warrior>(player).Count > 0
        && BuildGraveyardMagnetPool<Gamma_the_Magnet_Warrior>(player).Count > 0;

    private static List<BaseMonsterCard> BuildGraveyardMagnetPool<TMag>(Player player)
        where TMag : BaseMonsterCard =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<TMag>()
            .Where(m => m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate)
            .Cast<BaseMonsterCard>()
            .ToList();
}
