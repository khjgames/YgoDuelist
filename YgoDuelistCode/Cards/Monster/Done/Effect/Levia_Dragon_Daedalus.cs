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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Levia_Dragon_Daedalus : EffectMonsterCard, IMonsterActivatedEffect
{
    public Levia_Dragon_Daedalus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 26,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ocean | YgoCardPackTags.Water | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Ocean_Dragon_Lord_Neo_Daedalus) };

    public override Type[] RelatedCards => new[] { typeof(Levia_Dragon_Daedalus), typeof(Umi), typeof(A_Legendary_Ocean) };

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
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 1m) });

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-LEVIA_DRAGON_DAEDALUS.activated_effect.description";
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
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);
        }

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(Owner);
        CardPile? gy = YgoPlayerPiles.Graveyard(Owner);
        if (zone != null && gy != null)
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
        DynamicVars["Mgc2"].BaseValue = 2m;
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
