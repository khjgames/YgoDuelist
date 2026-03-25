using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class GraveyardRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/graveyard.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/graveyard.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetGraveyardCountForOwner();

    private CardPile? _subscribedPile;

    private readonly HashSet<string> _annualKeysConsumedThisTurn = new();

    private int _dragonMonstersDestroyedThisTurn;

    private bool _sanctuaryHalveNextSpillToPlayer;

    private int _sevenWeaponsCardsPlayed;

    private bool _sevenWeaponsBonusActive;

    private readonly List<BaseMonsterCard> _sevenWeaponsBonusCards = new();

    public override Task BeforeCombatStart()
    {
        SubscribeToGraveyardPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromGraveyardPile();
        _sanctuaryHalveNextSpillToPlayer = false;
        ResetSevenWeaponsCombatState();
        return Task.CompletedTask;
    }

    /// <summary>At the start of your turn, set your star count to 1 (from the graveyard).</summary>
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner?.Creature.Side)
            await PlayerCmd.SetStars(1, Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _annualKeysConsumedThisTurn.Clear();
            _dragonMonstersDestroyedThisTurn = 0;
            _sanctuaryHalveNextSpillToPlayer = false;
            YgoDarkSpiritSilentState.ClearForPlayer(player);
        }

        if (player != Owner || player.PlayerCombatState == null || player.Creature == null)
            return;

        if (YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<The_Sanctuary_in_the_Sky>(player)
            && TryConsumeAnnual("SANCTUARY_MERCURY_DRAW"))
        {
            bool hasMercury = false;
            foreach (Creature pet in player.PlayerCombatState.Pets)
            {
                if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is The_Agent_of_Wisdom_Mercury)
                {
                    hasMercury = true;
                    break;
                }
            }

            if (hasMercury)
                await CardPileCmd.Draw(choiceContext, 1, player);
        }

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            CardModel? src = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (src is not Cure_Mermaid mermaid)
                continue;
            string key = $"CURE_MERMAID_{pet.CombatId}";
            if (!TryConsumeAnnual(key))
                continue;
            decimal maintenance = mermaid.IsUpgraded ? 0m : 1m;
            if (maintenance > 0)
                await CreatureCmd.Damage(choiceContext, pet, maintenance, ValueProp.Move, player.Creature, mermaid);
            await CreatureCmd.Heal(player.Creature, 1m);
        }

        if (GetGraveyardCards(player).Any(c => c is Darklord_Marie) && TryConsumeAnnual("DARKLORD_MARIE_GY"))
            await CreatureCmd.Heal(player.Creature, 1m);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player && Owner?.PlayerCombatState != null)
            MonsterCommandRegistry.ClearPerTurnExtrasForPlayer(Owner);
        return Task.CompletedTask;
    }

    /// <summary>Once-per-turn (annual) gate keyed by string; returns true the first call each player turn.</summary>
    public bool TryConsumeAnnual(string key)
    {
        if (_annualKeysConsumedThisTurn.Contains(key))
            return false;
        _annualKeysConsumedThisTurn.Add(key);
        return true;
    }

    /// <summary>Chunk Z: Splinter (50% splash to other enemies) and Blight (50% of unblocked as stacks) after duel monster <see cref="AttackCommand"/>.</summary>
    public override async Task AfterAttack(AttackCommand command)
    {
        if (Owner == null || command.Attacker?.Player != Owner)
            return;

        if (!command.IsSingleTargeted || command.ModelSource is not BaseMonsterCard monster)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        var cs = command.Attacker.CombatState;
        if (cs == null)
            return;

        Player? atkOwner = command.Attacker.Player;
        if (monster is Spirit_of_the_Breeze && atkOwner?.Creature != null)
            await CreatureCmd.Heal(atkOwner.Creature, 1m);

        if (monster is D_D_Warrior_Lady warriorLady)
        {
            Creature? wlPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(warriorLady);
            if (wlPet != null)
                MonsterCommandRegistry.GetOrCreate(wlPet).WarriorLadyBanishWindowActive = true;
        }

        bool splinter = monster.AttackDealsSplinterDamage;
        if (!splinter)
        {
            foreach (var eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
            {
                if (eq is BaseEquipSpellCard be && be.GrantsSplinterDamage)
                {
                    splinter = true;
                    break;
                }
            }
        }

        if (splinter)
        {
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || r.UnblockedDamage <= 0)
                    continue;

                int splash = (int)decimal.Floor(r.UnblockedDamage * 0.5m);
                if (splash <= 0)
                    continue;

                foreach (Creature other in cs.GetOpponentsOf(command.Attacker))
                {
                    if (!other.IsAlive || other == r.Receiver)
                        continue;

                    await DamageCmd.Attack(splash)
                        .FromCard(monster)
                        .Targeting(other)
                        .WithHitFx("vfx/vfx_attack_slash")
                        .Execute(ctx);
                }
            }
        }

        bool blighted = monster.AttackDealsBlightedDamage;
        if (!blighted)
        {
            foreach (var eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
            {
                if (eq is BaseEquipSpellCard be && be.GrantsBlightTo(monster))
                {
                    blighted = true;
                    break;
                }
            }
        }

        if (blighted)
        {
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || r.UnblockedDamage <= 0)
                    continue;

                int blight = (int)decimal.Floor(r.UnblockedDamage * 0.5m);
                if (blight <= 0)
                    continue;

                await PowerCmd.Apply<BlightPower>(r.Receiver, blight, command.Attacker, monster);
            }
        }

        int killBonus = monster.PermanentAtkDeltaOnEnemyExecute;
        if (killBonus != 0)
        {
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                if (!monster.AppliesPermanentAtkDeltaOnEnemyKill(r.Receiver))
                    continue;
                monster.DynamicVars.Damage.BaseValue += killBonus;
            }
        }

        if (monster is Timeater)
        {
            bool executed = false;
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                executed = true;
                break;
            }

            if (executed)
            {
                foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive).ToList())
                    await CreatureCmd.Stun(e);
            }
        }

        Player? atkPlayer = command.Attacker.Player;
        if (atkPlayer?.Creature != null)
        {
            if (monster is Twin_Headed_Wolf && PlayerControlsAtLeastTwoFiendsOnField(atkPlayer))
            {
                foreach (DamageResult r in command.Results)
                {
                    if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                        continue;
                    await PowerCmd.Apply<StrengthPower>(command.Attacker, 1m, atkPlayer.Creature, monster);
                    await PowerCmd.Apply<ArtifactPower>(command.Attacker, 1m, atkPlayer.Creature, monster);
                }
            }

            bool anyUnblocked = false;
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side == CombatSide.Enemy && r.UnblockedDamage > 0)
                {
                    anyUnblocked = true;
                    break;
                }
            }

            if (anyUnblocked)
            {
                BaseEquipSpellCard? burningBeast = null;
                foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
                {
                    if (eq is Burning_Beast)
                    {
                        burningBeast = eq;
                        break;
                    }
                }

                if (burningBeast != null)
                {
                    foreach (DamageResult r in command.Results)
                    {
                        if (r.Receiver.Side != CombatSide.Enemy || r.UnblockedDamage <= 0)
                            continue;

                        await PowerCmd.Apply<WeakPower>(r.Receiver, 1m, atkPlayer.Creature, burningBeast);
                        await PowerCmd.Apply<VulnerablePower>(r.Receiver, 1m, atkPlayer.Creature, burningBeast);
                    }
                }

                foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
                {
                    if (eq is Cestus_of_Dagla)
                    {
                        await CreatureCmd.Heal(atkPlayer.Creature, 1m);
                        break;
                    }
                }

                if (monster is The_Bistro_Butcher butcher)
                {
                    int draw = butcher.IsUpgraded ? 2 : 1;
                    await CardPileCmd.Draw(ctx, draw, atkPlayer);
                }
            }
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Owner == null || cardPlay.Card.Owner != Owner)
            return;

        _sevenWeaponsCardsPlayed++;
        if (_sevenWeaponsBonusActive)
            StripSevenWeaponsBonus();

        if (_sevenWeaponsCardsPlayed % 7 == 0)
        {
            ApplySevenWeaponsBonus();
            _sevenWeaponsBonusActive = true;
        }

        await SyncSevenWeaponsCounterPetsAsync(Owner);
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner.Creature || !_sanctuaryHalveNextSpillToPlayer)
            return amount;

        _sanctuaryHalveNextSpillToPlayer = false;
        if (amount <= 0m)
            return amount;
        return amount / 2m;
    }

    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
    {
        if (Owner == null || attack.Attacker == null || attack.Attacker.Side != CombatSide.Enemy)
            return hitCount;
        if (attack.TargetSide != CombatSide.Player)
            return hitCount;
        if (!YgoDarkSpiritSilentState.ShouldDoubleAttack(Owner, attack.Attacker))
            return hitCount;
        return hitCount * 2;
    }

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || Owner.Creature != target || dealer == null || dealer.Side != CombatSide.Enemy)
            return amount;
        if (!props.HasFlag(ValueProp.Move))
            return amount;
        if (!YgoDarkSpiritSilentState.IsNegated(Owner, dealer))
            return amount;
        return 0m;
    }

    /// <summary>Gets the graveyard pile for the current combat player, or null if not in combat.</summary>
    public static CardPile? GetGraveyardPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
    }

    /// <summary>Graveyard card count for this relic's owner (for relic display).</summary>
    private int GetGraveyardCountForOwner()
    {
        var player = Owner;
        if (player == null)
            return 0;

        var pile = GetGraveyardPile(player);
        return pile?.Cards.Count ?? 0;
    }

    private void SubscribeToGraveyardPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var pile = GetGraveyardPile(player);
        if (pile == null)
            return;

        // Avoid double-subscription.
        if (_subscribedPile != null)
            UnsubscribeFromGraveyardPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnGraveyardContentsChanged;
        // Force initial refresh.
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromGraveyardPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnGraveyardContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnGraveyardContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    /// <summary>Cards in the graveyard pile for the given player (for relic click view).</summary>
    public static IReadOnlyList<CardModel> GetGraveyardCards(Player? player)
    {
        var pile = GetGraveyardPile(player);
        if (pile == null) return [];
        return pile.Cards.ToList();
    }

    public static bool IsGraveyardRelic(RelicModel? model) => model is GraveyardRelic;

    public static GraveyardRelic? AsGraveyard(RelicModel? model) => model as GraveyardRelic;

    /// <summary>
    /// When a Fairy duel monster with Die for You dies under The Sanctuary in the Sky, the next spill damage
    /// to the player from that same hit is halved (see <see cref="ModifyHpLostAfterOsty"/>).
    /// </summary>
    public static void ArmSanctuaryHalveNextSpillDamage(Player? player)
    {
        GraveyardRelic? g = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        if (g != null)
            g._sanctuaryHalveNextSpillToPlayer = true;
    }

    public static async Task SyncSevenWeaponsCounterPetsAsync(Player? player)
    {
        if (player?.PlayerCombatState == null || player.Creature == null)
            return;

        GraveyardRelic? g = player.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        int display = g?.GetSevenWeaponsCounterDisplay() ?? 1;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not The_Hunter_with_7_Weapons hw)
                continue;

            await PowerCmd.Remove<SevenWeaponsPower>(pet);
            await PowerCmd.Apply<SevenWeaponsPower>(pet, display, player.Creature, hw);
        }
    }

    /// <summary>If a Hunter is summoned while the 7th-step ATK bonus is active, it receives the same bonus.</summary>
    public void OnHunterSummonedDuringSevenWeaponsBonus(The_Hunter_with_7_Weapons hunterCard)
    {
        if (!_sevenWeaponsBonusActive)
            return;
        int d = SevenWeaponsAtkDelta(hunterCard);
        hunterCard.DynamicVars.Damage.BaseValue += d;
        _sevenWeaponsBonusCards.Add(hunterCard);
    }

    private int GetSevenWeaponsCounterDisplay()
    {
        if (_sevenWeaponsCardsPlayed == 0)
            return 1;
        int m = _sevenWeaponsCardsPlayed % 7;
        return m == 0 ? 7 : m;
    }

    private static int SevenWeaponsAtkDelta(The_Hunter_with_7_Weapons w) => w.IsUpgraded ? 12 : 10;

    private void ApplySevenWeaponsBonus()
    {
        foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.GetFieldMonsters(Owner))
        {
            if (m is not The_Hunter_with_7_Weapons w)
                continue;
            int d = SevenWeaponsAtkDelta(w);
            w.DynamicVars.Damage.BaseValue += d;
            _sevenWeaponsBonusCards.Add(w);
        }
    }

    private void StripSevenWeaponsBonus()
    {
        foreach (BaseMonsterCard m in _sevenWeaponsBonusCards)
        {
            if (m is The_Hunter_with_7_Weapons w)
                w.DynamicVars.Damage.BaseValue -= SevenWeaponsAtkDelta(w);
        }

        _sevenWeaponsBonusCards.Clear();
        _sevenWeaponsBonusActive = false;
    }

    private void ResetSevenWeaponsCombatState()
    {
        StripSevenWeaponsBonus();
        _sevenWeaponsCardsPlayed = 0;
    }

    public static void RegisterDragonMonsterDestroyed(Player? player)
    {
        GraveyardRelic? g = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        g?.NotifyDragonMonsterDestroyed();
    }

    private void NotifyDragonMonsterDestroyed() => _dragonMonstersDestroyedThisTurn++;

    /// <summary>Dragon duel monsters destroyed during the current player turn (for Super Rejuvenation). Cleared at the start of your next turn.</summary>
    public int DragonMonstersDestroyedThisTurn => _dragonMonstersDestroyedThisTurn;

    private static bool PlayerControlsAtLeastTwoFiendsOnField(Player player)
    {
        int fiends = 0;
        foreach (BaseMonsterCard c in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (c.DuelMonsterRace == DuelMonsterRace.Fiend)
                fiends++;
        }

        return fiends >= 2;
    }
}
