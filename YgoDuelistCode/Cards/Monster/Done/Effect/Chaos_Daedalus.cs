using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
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
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 LIGHT and 1 DARK from your Graveyard to Special Summon.
/// Activate (once per turn) while you control a face-up Field Spell: banish 1 card from your Spell/Trap or Monster Zones; gain {Mgc} Chaotic Evolution.
/// </summary>
public sealed class Chaos_Daedalus : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishLightPrompt =
        new("cards", "YGODUELIST-CHAOS_DAEDALUS.banish_light_graveyard");

    private static readonly LocString BanishDarkPrompt =
        new("cards", "YGODUELIST-CHAOS_DAEDALUS.banish_dark_graveyard");

    private static readonly LocString BanishOneFromYourZonesPrompt =
        new("cards", "YGODUELIST-CHAOS_DAEDALUS.banish_one_from_your_zones");

    public Chaos_Daedalus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 26,
            baseDef: 15,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Chaos_Daedalus), typeof(Chaos_Sorcerer) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<ChaoticEvolutionPower>();
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
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-CHAOS_DAEDALUS.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            if (!YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(Owner).Any())
                return false;
            return BuildBanishOneFromYourZonesCandidates(Owner, this).Count >= 1;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Chaos_Daedalus || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || player.Creature == null)
            return;

        if (!YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(player).Any())
            return;

        List<CardModel> candidates = BuildBanishOneFromYourZonesCandidates(player, (Chaos_Daedalus)source);
        if (candidates.Count < 1)
            return;

        CardModel? pick = await YgoOrderedCardSelection.TryChooseSingleAsync<CardModel>(
            choiceContext,
            player,
            new CardSelectorPrefs(BanishOneFromYourZonesPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => candidates);
        if (pick == null || !candidates.Contains(pick))
            return;

        if (pick is BaseMonsterCard bm && DuelMonsterFieldRegistry.ContainsFieldMonster(player, bm))
        {
            Creature? targetPet = YgoMpCombatOrder.FirstPetWhere(
                player.PlayerCombatState!,
                p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, bm));
            if (targetPet == null)
                return;
            await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToBanishedAsync(player, targetPet, bm);
        }
        else if (pick is BaseSpellCard or BaseTrapCard)
        {
            YgoSpellTrapEquipLinkRegistry.Detach(pick);
            await YgoBanishedService.BanishCard(player, pick);
            YgoSpellTrapZoneBridge.SyncFromZonePile(player);
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
        }
        else
        {
            return;
        }

        int stacks = (int)source.DynamicVars["Mgc"].BaseValue;
        if (stacks > 0)
            await PowerCmd.Apply<ChaoticEvolutionPower>(selfPet, stacks, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
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
            .Where(m => m.DuelMonsterAttribute == attribute && (exclude == null || !ReferenceEquals(m, exclude)))
            .ToList();

    private static List<CardModel> BuildBanishOneFromYourZonesCandidates(Player player, Chaos_Daedalus excludeSource)
    {
        var list = new List<CardModel>();
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards))
            {
                if (c is BaseSpellCard or BaseTrapCard)
                    list.Add(c);
            }
        }

        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, excludeSource))
                continue;
            list.Add(m);
        }

        return list;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
