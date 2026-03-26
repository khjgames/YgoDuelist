using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Revised: damage all enemies for (sum of their attack intents) × 4 (×5 upgraded); destroy Spell/Trap zone and hand except Normal monsters;
/// destroy non-Normal monsters on your field (pets killed, unsummoned cards to GY).
/// </summary>
public sealed class The_Law_of_the_Normal : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 4m) };

    public The_Law_of_the_Normal()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        var cs = player.Creature.CombatState;
        var enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        int combined = 0;
        foreach (Creature e in enemies)
            combined += YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, player.Creature);

        int mult = (int)DynamicVars["Mgc"].BaseValue;
        decimal dmgEach = combined * mult;
        if (dmgEach > 0)
        {
            foreach (Creature enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;
                await CreatureCmd.Damage(choiceContext, enemy, dmgEach, ValueProp.Unpowered, player.Creature, this);
            }
        }

        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (gy == null)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone != null && zone.Cards.Count > 0)
        {
            List<CardModel> zoneCards = zone.Cards.ToList();
            await CardPileCmd.Add(zoneCards, gy, CardPilePosition.Top, this, false);
            YgoSpellTrapZoneBridge.SyncFromZonePile(player);
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
        }

        foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.GetFieldMonsters(player).ToList())
        {
            if (m.YgoCardType == YgoCardType.Monster)
                continue;
            Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
            if (pet != null && pet.IsAlive)
                await CreatureCmd.Kill(pet, force: true);
        }

        CardPile? monsterPile = MonsterPile.CustomType.GetPile(player);
        if (monsterPile != null)
        {
            foreach (CardModel c in monsterPile.Cards.ToList())
            {
                if (c is not BaseMonsterCard bm || bm.YgoCardType == YgoCardType.Monster)
                    continue;
                Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, bm);
                if (pet == null && c.Pile != gy)
                    await CardPileCmd.Add(new[] { c }, gy, CardPilePosition.Top, this, false);
            }
        }

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null)
        {
            foreach (CardModel c in hand.Cards.ToList())
            {
                if (ReferenceEquals(c, this))
                    continue;
                if (c is BaseMonsterCard bm && bm.YgoCardType == YgoCardType.Monster)
                    continue;
                if (c.Pile != gy)
                    await CardPileCmd.Add(new[] { c }, gy, CardPilePosition.Top, this, false);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
