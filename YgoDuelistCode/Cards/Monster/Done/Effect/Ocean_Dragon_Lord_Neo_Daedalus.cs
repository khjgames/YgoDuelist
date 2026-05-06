using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Ocean_Dragon_Lord_Neo_Daedalus : EffectMonsterCard, IMonsterActivatedEffect
{
    public Ocean_Dragon_Lord_Neo_Daedalus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 29,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.None;

    public override Type[] RelatedCards => new[] { typeof(Ocean_Dragon_Lord_Neo_Daedalus), typeof(Levia_Dragon_Daedalus) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    /// <summary>Blight multiplier for the activated effect (<c>ATK × Mgc2</c>).</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 2m) });

    protected override bool SupportsHandEffectForm => true;
    public override bool CanSummonDuelMonster => false;
    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;
    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;
    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable
        && (!IsHandEffectFormActive || CanTributeLeviaForHandSummon());

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            if (Owner == null || Owner.PlayerCombatState == null)
                return;
            Creature? leviaPet = YgoMpCombatOrder.FirstPetWhere(
                Owner.PlayerCombatState,
                p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceMonster<Levia_Dragon_Daedalus>(p) != null);
            if (leviaPet == null)
                return;
            await CreatureCmd.Kill(leviaPet, force: true);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(Owner, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-OCEAN_DRAGON_LORD_NEO_DAEDALUS.activated_effect.description";
    public bool IsActivatedEffectAvailable => Owner != null && HasUmiLikeFieldSpell(Owner);

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (!await TrySendUmiLikeFieldSpellToGraveyardAsync(Owner))
            return;

        int atk = (int)NormalMonsterCard.GetTotalAtkForPreview(this);
        int blight = (int)(atk * DynamicVars["Mgc2"].BaseValue);
        if (blight > 0)
        {
            foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
                await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
        }

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!pet.IsAlive || DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);
        }

        CardPile? gy = YgoPlayerPiles.Graveyard(Owner);
        CardPile? hand = YgoPlayerPiles.Hand(Owner);
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(Owner);
        if (gy != null && hand != null && hand.Cards.Count > 0)
            await CardPileCmd.Add(YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards), gy, CardPilePosition.Top, this, false);
        if (gy != null && zone != null && zone.Cards.Count > 0)
        {
            IEnumerable<CardModel> zoneToGy = YgoDuelMonsterDestructionRules.FilterSpellTrapZoneCardsForMassDestroy(
                Owner,
                YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards),
                YgoDestructionSourceKind.MonsterEffect);
            await CardPileCmd.Add(zoneToGy, gy, CardPilePosition.Top, this, false);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc2"].BaseValue = 3m;
    }

    private bool CanTributeLeviaForHandSummon()
    {
        if (Owner?.PlayerCombatState == null || !DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
            return false;
        return YgoMpCombatOrder.PetsAny(
            Owner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceMonster<Levia_Dragon_Daedalus>(p) != null)
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(Owner, DuelMonsterRace.SeaSerpent);
    }

    private static bool HasUmiLikeFieldSpell(MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Umi>(player)
        || YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<A_Legendary_Ocean>(player);

    private static async Task<bool> TrySendUmiLikeFieldSpellToGraveyardAsync(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (zone == null || gy == null)
            return false;
        CardModel? card = YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Umi or A_Legendary_Ocean);
        if (card == null)
            return false;
        await CardPileCmd.Add(new[] { card }, gy, CardPilePosition.Top, card, false);
        return true;
    }
}
