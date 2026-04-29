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
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 LIGHT and 1 DARK from your Graveyard to Special Summon.
/// Activate: destroy your entire hand, Spell/Trap Zone, and all duel monsters you control (including this card);
/// inflict {Mgc} × (number of cards and monsters destroyed) Blight on all enemies (once per turn).
/// </summary>
public sealed class Chaos_Emperor_Dragon_Envoy_of_the_End : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishLightPrompt =
        new("cards", "YGODUELIST-CHAOS_EMPEROR_DRAGON_ENVOY_OF_THE_END.banish_light_graveyard");

    private static readonly LocString BanishDarkPrompt =
        new("cards", "YGODUELIST-CHAOS_EMPEROR_DRAGON_ENVOY_OF_THE_END.banish_dark_graveyard");

    public Chaos_Emperor_Dragon_Envoy_of_the_End()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 30,
            baseDef: 25,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Light | YgoCardPackTags.Dragon | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Chaos_Emperor_Dragon_Envoy_of_the_End), typeof(Chaos_Sorcerer) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;

            BaseMonsterCard? light = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishLightPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Light));
            if (light == null)
                return;

            BaseMonsterCard? dark = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishDarkPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Dark, exclude: light));
            if (dark == null)
                return;

            await YgoBanishedService.BanishCard(player, light);
            await YgoBanishedService.BanishCard(player, dark);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-CHAOS_EMPEROR_DRAGON_ENVOY_OF_THE_END.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            return pet != null
                && pet.IsAlive
                && MonsterCommandRegistry.TryGet(pet, out var cmd)
                && !cmd.HasUsedActivatedEffectThisTurn;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = choiceContext;
        _ = cardPlay;
        if (source is not Chaos_Emperor_Dragon_Envoy_of_the_End || Owner?.Creature?.CombatState is not { } cs)
            return;

        Player player = Owner;
        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || !selfPet.IsAlive || player.Creature == null || player.PlayerCombatState == null)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        int handCount = hand?.Cards.Count ?? 0;
        List<Creature> duelPets = YgoMpCombatOrder
            .PetsSnapshotOrderedByCombatId(player.PlayerCombatState)
            .Where(p => p.IsAlive && p.Monster is DuelMonsterModel)
            .ToList();
        List<CardModel> zoneToGy = zone == null
            ? new List<CardModel>()
            : YgoDuelMonsterDestructionRules
                .FilterSpellTrapZoneCardsForMassDestroy(
                    player,
                    YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards),
                    YgoDestructionSourceKind.MonsterEffect)
                .ToList();
        int zoneCount = zoneToGy.Count;
        List<Creature> otherPets = duelPets.Where(p => !ReferenceEquals(p, selfPet)).ToList();
        List<Creature> otherPetsToKill = YgoDuelMonsterDestructionRules
            .FilterPetsForMassKill(otherPets, YgoDestructionSourceKind.MonsterEffect)
            .ToList();
        int destroyedCount = handCount + zoneCount + otherPetsToKill.Count + 1;
        decimal blightEach = DynamicVars["Mgc"].BaseValue * destroyedCount;
        if (blightEach < 0m)
            blightEach = 0m;

        if (gy != null && hand != null && handCount > 0)
            await CardPileCmd.Add(
                YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards),
                gy,
                CardPilePosition.Top,
                this,
                false);
        if (gy != null && zone != null && zoneCount > 0)
            await CardPileCmd.Add(zoneToGy, gy, CardPilePosition.Top, this, false);

        foreach (Creature pet in otherPetsToKill)
        {
            if (!pet.IsAlive)
                continue;
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);
        }

        if (blightEach > 0m)
        {
            foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
                await PowerCmd.Apply<BlightPower>(enemy, blightEach, player.Creature, source);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);

        if (selfPet.IsAlive)
            await CreatureCmd.Kill(selfPet, force: true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 6m;
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Light).Count >= 1
            && BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Dark).Count >= 1;
    }

    private static List<BaseMonsterCard> BuildGraveyardAttributeCandidates(
        Player player,
        DuelMonsterAttribute attribute,
        BaseMonsterCard? exclude = null) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m.DuelMonsterAttribute == attribute
                && (exclude == null || !ReferenceEquals(m, exclude))
                && m is not Chaos_Emperor_Dragon_Envoy_of_the_End)
            .ToList();
}
