using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

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

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Ocean | YgoCardPackTags.Water;

    public override Type[] RelatedCards => new[] { typeof(Ocean_Dragon_Lord_Neo_Daedalus), typeof(Levia_Dragon_Daedalus) };

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
            Creature? leviaPet = Owner.PlayerCombatState.Pets
                .FirstOrDefault(p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is Levia_Dragon_Daedalus);
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
            foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
        }

        foreach (Creature pet in Owner.PlayerCombatState?.Pets?.ToList() ?? Enumerable.Empty<Creature>())
        {
            if (!pet.IsAlive || DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == this)
                continue;
            await CreatureCmd.Kill(pet, force: true);
        }

        CardPile? gy = GraveyardPile.CustomType.GetPile(Owner);
        CardPile? hand = PileType.Hand.GetPile(Owner);
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(Owner);
        if (gy != null && hand != null && hand.Cards.Count > 0)
            await CardPileCmd.Add(hand.Cards.ToList(), gy, CardPilePosition.Top, this, false);
        if (gy != null && zone != null && zone.Cards.Count > 0)
            await CardPileCmd.Add(zone.Cards.ToList(), gy, CardPilePosition.Top, this, false);
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
        return Owner.PlayerCombatState.Pets.Any(p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is Levia_Dragon_Daedalus);
    }

    private static bool HasUmiLikeFieldSpell(MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Umi>(player)
        || YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<A_Legendary_Ocean>(player);

    private static async Task<bool> TrySendUmiLikeFieldSpellToGraveyardAsync(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (zone == null || gy == null)
            return false;
        CardModel? card = zone.Cards.FirstOrDefault(c => c is Umi or A_Legendary_Ocean);
        if (card == null)
            return false;
        await CardPileCmd.Add(new[] { card }, gy, CardPilePosition.Top, card, false);
        return true;
    }
}
