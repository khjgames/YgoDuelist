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
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
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

    public override Task BeforeCombatStart()
    {
        SubscribeToGraveyardPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromGraveyardPile();
        return Task.CompletedTask;
    }

    /// <summary>At the start of your turn, set your star count to 1 (from the graveyard).</summary>
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner?.Creature.Side)
            await PlayerCmd.SetStars(1, Owner);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _annualKeysConsumedThisTurn.Clear();
            _dragonMonstersDestroyedThisTurn = 0;
        }
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

    /// <summary>Splinter / Blighted resolution for duel monster attacks.</summary>
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

        if (monster.AttackDealsBlightedDamage)
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
                monster.DynamicVars.Damage.BaseValue += killBonus;
            }
        }

        Player? atkPlayer = command.Attacker.Player;
        if (atkPlayer?.Creature != null)
        {
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
                foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
                {
                    if (eq is Cestus_of_Dagla)
                    {
                        await CreatureCmd.Heal(atkPlayer.Creature, 1m);
                        break;
                    }
                }
            }
        }
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

    public static void RegisterDragonMonsterDestroyed(Player? player)
    {
        GraveyardRelic? g = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        g?.NotifyDragonMonsterDestroyed();
    }

    private void NotifyDragonMonsterDestroyed() => _dragonMonstersDestroyedThisTurn++;

    /// <summary>Dragon duel monsters destroyed during the current player turn (for Super Rejuvenation). Cleared at the start of your next turn.</summary>
    public int DragonMonstersDestroyedThisTurn => _dragonMonstersDestroyedThisTurn;
}
