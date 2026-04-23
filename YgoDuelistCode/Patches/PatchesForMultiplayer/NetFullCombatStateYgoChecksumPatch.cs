using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;
using Godot;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Vanilla <see cref="NetFullCombatState.FromRun"/> feeds <see cref="ChecksumTracker.GenerateChecksum"/>, which hashes
/// the raw packet bytes. List iteration order for creatures' powers, players, pile cards, etc. can differ
/// across peers even when game state matches. YGO also uses custom <see cref="CardPile"/>s that must be included for
/// Duelist players. This postfix makes serialization order deterministic and appends all relevant YGO combat piles.
/// <para>
/// <see cref="MonsterCommandCard"/> in <see cref="YgoCardOptionPile"/> (duel monster menu) are host/local UI and are
/// omitted from the checksum snapshot so peers agree.
/// </para>
/// <para>
/// <see cref="NetFullCombatState.nextChoiceIds"/> (per-slot <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer"/>
/// counters) can differ between host and client without real gameplay divergence; they are omitted from the MP xxhash in
/// <see cref="ChecksumTrackerMpStripEphemeralChoiceIdsPatch"/>.
/// </para>
/// </summary>
[HarmonyPatch(typeof(NetFullCombatState), nameof(NetFullCombatState.FromRun))]
public static class NetFullCombatStateYgoChecksumPatch
{
    /// <summary>Set true temporarily to log stabilization steps (verbose during combat).</summary>
    public static bool VerboseChecksumLog;

    /// <summary>
    /// When true, logs a deterministic one-line fingerprint of YgoDuelist pile snapshots after stabilization (MP debug).
    /// </summary>
    public static bool EnableChecksumFingerprintLog = true;

    /// <summary>
    /// Run before combat state is snapshotted for checksums. Cure Mermaid forced Die For You and optional toggle
    /// (<see cref="BaseMonsterCard.YgoDieForYouUserToggleOn"/>) must produce the same <see cref="DieForYouPower"/> on every peer.
    /// Uses <see cref="MonsterCommandRegistry.ApplyDieForYouSyncForChecksum"/> — do not call PowerCmd.Apply with
    /// <c>GetResult()</c> here (awaits timed waits; caused host/client divergence on Die For You at checksum time).
    /// <para>
    /// Duel pet stance powers (attack/defense/face-down markers on the pet) are driven by async
    /// <see cref="DuelMonsterStancePowerSync.RequestSyncIfSummoned"/>; reconcile from field cards before snapshot so host/client
    /// match (divergence after e.g. <see cref="Command_Attack"/>).
    /// </para>
    /// <para>
    /// Face-down overlay keyword <c>10012</c> on monsters/spells/traps must match <see cref="AbstractMonsterCard.FaceDown"/> /
    /// trap presentation flags before snapshot; otherwise one peer can still have the keyword in <see cref="CardModel.Keywords"/>
    /// after a UI tick and diverge at &quot;After player turn start&quot; checksums.
    /// Hand/play piles force-recompute <see cref="AbstractMonsterCard.FaceDown"/> (do not preserve stale <c>true</c>) so observers
    /// cannot diverge after hover/set visuals on monsters still in hand.
    /// </para>
    /// <para>
    /// <see cref="YgoDuelistPowerChecksumReconcile.ReconcileAll"/> aligns <see cref="YgoEquipSpellRegistry"/> and
    /// <see cref="YgoSpellTrapEquipLinkRegistry"/> with zone + pet ids, strips orphan trap-linked powers (e.g. <see cref="SpellbindingCircleTargetPower"/>),
    /// and invokes <see cref="YgoDuelistPower.ReconcileForMpChecksumSnapshot"/> on every <see cref="YgoDuelistPower"/> instance.
    /// </para>
    /// </summary>
    [HarmonyPrefix]
    public static void Prefix(IRunState runState, GameAction? justFinishedAction)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        try
        {
            ReconcileFaceDownPresentationBeforeSnapshot(runState);
            ReconcileDuelPetDieForYouBeforeSnapshot(runState);
            ReconcileDuelPetStancePowersBeforeSnapshot(runState);
            YgoDuelistPowerChecksumReconcile.ReconcileAll(runState, VerboseChecksumLog);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP][Checksum] Prefix reconcile failed: {ex.Message}");
        }
    }

    /// <summary>
    /// <see cref="ChecksumTracker"/> always snapshots via <see cref="NetFullCombatState.FromRun"/> — including map moves,
    /// events, shops (combat not in progress). Vanilla iterates <see cref="IRunState.Players"/> in collection order, which
    /// can differ between host and client; we always sort and stabilize serialized lists so the packet hash matches.
    /// YGO pile appends and face-down reconciles stay combat-only.
    /// <para>
    /// Do <b>not</b> sort <see cref="NetFullCombatState.nextChoiceIds"/> by numeric value: indices are player slot indices
    /// (see <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer"/>), and sorting would swap
    /// counters between players.
    /// </para>
    /// </summary>
    public static void Postfix(IRunState runState, GameAction? justFinishedAction, ref NetFullCombatState __result)
    {
        bool inCombat = CombatManager.Instance?.IsInProgress == true;

        List<NetFullCombatState.PlayerState> players = __result.Players;

        // Same player ordering on every peer (runState.Players order is not guaranteed to match).
        players.Sort((a, b) => a.playerId.CompareTo(b.playerId));

        for (int i = 0; i < players.Count; i++)
        {
            NetFullCombatState.PlayerState ps = players[i];
            if (inCombat)
            {
                Player? pl = FindPlayer(runState, ps.playerId);
                if (pl?.Character is YgoChar)
                    AppendYgoCustomCombatPiles(pl, ref ps);
            }

            StabilizePlayerStateForChecksum(ref ps);
            StripEphemeralYgoPresentationKeywordsFromChecksumPiles(ref ps);
            players[i] = ps;
        }

        StabilizeCreaturesForChecksum(__result);
        if (VerboseChecksumLog && inCombat && __result.Creatures is { Count: > 0 })
        {
            var crea = __result.Creatures
                .OrderBy(CreatureSortKeyForLog)
                .Select(c =>
                    $"{c.monsterId?.Entry ?? "?"}:{c.currentHp}/{c.maxHp}b{c.block}p{c.powers?.Count ?? 0}");
            GD.Print($"[YgoDuelist][MP][Checksum][creatures] {string.Join(" | ", crea)}");
        }

        if (VerboseChecksumLog && !inCombat)
        {
            string ch = __result.nextChoiceIds is { Count: > 0 }
                ? string.Join(",", __result.nextChoiceIds)
                : "";
            GD.Print($"[YgoDuelist][MP][Checksum][noncombat] choices=[{ch}] players={players.Count}");
        }

        if (!inCombat)
            return;

        if (EnableChecksumFingerprintLog)
        {
            for (int i = 0; i < players.Count; i++)
            {
                NetFullCombatState.PlayerState ps = players[i];
                Player? pl = FindPlayer(runState, ps.playerId);
                if (pl?.Character is YgoChar)
                    GD.Print($"[YgoDuelist][MP][Checksum][fp] {BuildYgoPlayerPilesFingerprint(ps)}");
            }
        }

        if (VerboseChecksumLog)
        {
            GD.Print(
                $"[YgoDuelist][MP][Checksum] stabilized: players={__result.Players.Count} creatures={__result.Creatures.Count} choices={__result.nextChoiceIds?.Count ?? 0}");
        }
    }

    private static void ReconcileFaceDownPresentationBeforeSnapshot(IRunState runState)
    {
        int monsters = 0;
        int traps = 0;

        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.PlayerCombatState == null)
                continue;

            foreach (CardPile pile in player.PlayerCombatState.AllPiles)
                ReconcileFaceDownKeywordsOnPileCards(pile, ref monsters, ref traps);

            ReconcileFaceDownOnYgoCustomPiles(player, ref monsters, ref traps);
        }

        if (VerboseChecksumLog)
        {
            GD.Print(
                $"[YgoDuelist][MP][Checksum] FaceDown keyword reconcile: monsters={monsters} traps={traps}");
        }
    }

    /// <summary>Trap face presentation keyword (see <see cref="BaseTrapCard"/>); must never appear on monsters — pooled UI can leave it in <see cref="CardModel.Keywords"/> and diverge checksums (e.g. vs <see cref="AbstractMonsterCard"/> face-down 10012).</summary>
    private static readonly CardKeyword TrapPresentationKeyword = (CardKeyword)10011;

    private static void ReconcileFaceDownKeywordsOnPileCards(CardPile pile, ref int monsters, ref int traps)
    {
        bool handOrPlay = pile.Type == PileType.Hand || pile.Type == PileType.Play;
        foreach (CardModel card in pile.Cards)
        {
            if (card is AbstractMonsterCard amc)
            {
                amc.RemoveKeyword(TrapPresentationKeyword);
                amc.NormalizeFaceDownStateForCurrentDisplayMode(
                    requestStanceSyncAfterKeyword: false,
                    preserveExplicitFaceDownWhenAlreadyTrue: !handOrPlay);
                monsters++;
            }
            else if (card is BaseTrapCard btc)
            {
                btc.SyncFaceDownPresentationKeyword();
                traps++;
            }
        }
    }

    private static void ReconcileFaceDownOnYgoCustomPiles(Player player, ref int monsters, ref int traps)
    {
        CardPile? opt = YgoPlayerPiles.OptionPile(player);
        if (opt != null)
            ReconcileFaceDownKeywordsOnPileCards(opt, ref monsters, ref traps);
        CardPile? st = YgoPlayerPiles.SpellTrapZone(player);
        if (st != null)
            ReconcileFaceDownKeywordsOnPileCards(st, ref monsters, ref traps);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy != null)
            ReconcileFaceDownKeywordsOnPileCards(gy, ref monsters, ref traps);
        CardPile? mon = YgoPlayerPiles.MonsterZone(player);
        if (mon != null)
            ReconcileFaceDownKeywordsOnPileCards(mon, ref monsters, ref traps);
        CardPile? field = YgoPlayerPiles.Field(player);
        if (field != null)
            ReconcileFaceDownKeywordsOnPileCards(field, ref monsters, ref traps);
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        if (extra != null)
            ReconcileFaceDownKeywordsOnPileCards(extra, ref monsters, ref traps);
        CardPile? banished = YgoPlayerPiles.Banished(player);
        if (banished != null)
            ReconcileFaceDownKeywordsOnPileCards(banished, ref monsters, ref traps);
    }

    private static void ReconcileDuelPetDieForYouBeforeSnapshot(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.PlayerCombatState == null || player.Creature == null)
                continue;

            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                    continue;

                if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard bm)
                    continue;

                if (bm.ReconcileDieForYouChecksumForPet(pet, player))
                    continue;

                if (MonsterCommandRegistry.TryGet(pet, out var forcedReg) && forcedReg.DieForYouForced)
                    continue;

                if (bm.YgoDieForYouUserToggleOn && !pet.HasPower<DieForYouPower>())
                {
                    MonsterCommandRegistry.GetOrCreate(pet).DieForYouEnabled = true;
                    MonsterCommandRegistry.ApplyDieForYouSyncForChecksum(pet, player.Creature, bm);
                    GD.Print(
                        $"[YgoDuelist][MP][DieForYou] Reconciled optional toggle ON from YgoDieForYouUserToggleOn (playerNetId={player.NetId} source={bm.Id?.Entry})");
                }
            }
        }
    }

    private static void ReconcileDuelPetStancePowersBeforeSnapshot(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.PlayerCombatState == null || player.Creature == null)
                continue;

            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                    continue;

                if (DuelMonsterFieldRegistry.GetSourceMonster<AbstractMonsterCard>(pet) is not AbstractMonsterCard amc)
                    continue;

                DuelMonsterStancePowerSync.ApplyStanceFromSourceCardSyncForChecksum(pet, amc, player.Creature);
                SevenWeaponsHunterState.ApplySyncForChecksumIfHunter(pet, player);
            }
        }
    }

    private static Player? FindPlayer(IRunState runState, ulong netId)
    {
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (p.NetId == netId)
                return p;
        }

        return null;
    }

    private static void AppendYgoCustomCombatPiles(Player player, ref NetFullCombatState.PlayerState ps)
    {
        int before = ps.piles.Count;
        CardPile? opt = YgoPlayerPiles.OptionPile(player);
        CardPile? st = YgoPlayerPiles.SpellTrapZone(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        CardPile? mon = YgoPlayerPiles.MonsterZone(player);
        CardPile? field = YgoPlayerPiles.Field(player);
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        CardPile? banished = YgoPlayerPiles.Banished(player);
        TryAppendYgoOptionPileForChecksum(ref ps, opt);
        TryAppendPile(ref ps, st);
        TryAppendPile(ref ps, gy);
        TryAppendPile(ref ps, mon);
        TryAppendPile(ref ps, field);
        TryAppendPile(ref ps, extra);
        TryAppendPile(ref ps, banished);
        if (VerboseChecksumLog)
        {
            int added = ps.piles.Count - before;
            GD.Print(
                $"[YgoDuelist][MP][Checksum] YGO zones serialized +{added} pile row(s) netId={player.NetId} " +
                $"(nulls: opt={opt == null} st={st == null} gy={gy == null} mon={mon == null} field={field == null} extra={extra == null} banished={banished == null}; " +
                $"counts: opt={opt?.Cards.Count ?? -1} (menu commands excluded from checksum) st={st?.Cards.Count ?? -1} gy={gy?.Cards.Count ?? -1} mon={mon?.Cards.Count ?? -1} " +
                $"field={field?.Cards.Count ?? -1} extra={extra?.Cards.Count ?? -1} banished={banished?.Cards.Count ?? -1})");
        }
    }

    /// <summary>
    /// Duel monster option menu cards (<see cref="MonsterCommandCard"/>) are not replicated to the client the same way as
    /// host-local UI state; omit them from the snapshot so host/client checksum bytes match.
    /// </summary>
    private static void TryAppendYgoOptionPileForChecksum(ref NetFullCombatState.PlayerState ps, CardPile? pile)
    {
        if (pile == null)
            return;

        var combatPile = new NetFullCombatState.CombatPileState
        {
            pileType = pile.Type,
            cards = new List<NetFullCombatState.CardState>()
        };

        int skippedMenuCommands = 0;
        foreach (var card in pile.Cards)
        {
            if (card is MonsterCommandCard)
            {
                skippedMenuCommands++;
                continue;
            }

            combatPile.cards.Add(NetFullCombatState.CardState.From(card));
        }

        if (VerboseChecksumLog && skippedMenuCommands > 0)
        {
            GD.Print(
                $"[YgoDuelist][MP][Checksum] YgoCardOptionPile: excluded {skippedMenuCommands} MonsterCommandCard(s) from checksum snapshot (pileType={(int)pile.Type})");
        }

        ps.piles.Add(combatPile);
    }

    /// <summary>
    /// Must include empty custom zones: if one peer skips empty piles and the other has cards, checksum bytes diverge.
    /// </summary>
    private static void TryAppendPile(ref NetFullCombatState.PlayerState ps, CardPile? pile)
    {
        if (pile == null)
            return;
        ps.piles.Add(NetFullCombatState.CombatPileState.From(pile));
    }

    private static string BuildYgoPlayerPilesFingerprint(NetFullCombatState.PlayerState ps)
    {
        var parts = new List<string> { $"netId={ps.playerId}" };
        foreach (NetFullCombatState.CombatPileState pile in ps.piles.OrderBy(p => (int)p.pileType))
        {
            var cardKeys = pile.cards
                .Select(CardStateSortKey)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            parts.Add($"pt={(int)pile.pileType} n={pile.cards.Count}:{string.Join(",", cardKeys)}");
        }

        return string.Join(" | ", parts);
    }

    private static void StabilizePlayerStateForChecksum(ref NetFullCombatState.PlayerState ps)
    {
        StabilizeKeywordOrderInPiles(ref ps);
        SortCardStatesInAllPiles(ref ps);

        if (ps.piles.Count > 1)
            ps.piles.Sort((a, b) => ((int)a.pileType).CompareTo((int)b.pileType));

        if (ps.potions.Count > 1)
            ps.potions.Sort((a, b) => string.CompareOrdinal(a.id.Entry, b.id.Entry));

        if (ps.relics.Count > 1)
            ps.relics.Sort(CompareRelicState);

        if (ps.orbs.Count > 1)
        {
            ps.orbs.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.id.Entry, b.id.Entry);
                if (c != 0)
                    return c;
                c = a.passive.CompareTo(b.passive);
                if (c != 0)
                    return c;
                return a.evoke.CompareTo(b.evoke);
            });
        }
    }

    private static int CompareRelicState(NetFullCombatState.RelicState a, NetFullCombatState.RelicState b)
    {
        int c = string.CompareOrdinal(a.relic?.Id?.Entry ?? "", b.relic?.Id?.Entry ?? "");
        if (c != 0)
            return c;
        return (a.relic?.FloorAddedToDeck ?? -1).CompareTo(b.relic?.FloorAddedToDeck ?? -1);
    }

    private static void SortCardStatesInAllPiles(ref NetFullCombatState.PlayerState ps)
    {
        List<NetFullCombatState.CombatPileState> piles = ps.piles;
        for (int pi = 0; pi < piles.Count; pi++)
        {
            NetFullCombatState.CombatPileState pile = piles[pi];
            List<NetFullCombatState.CardState> cards = pile.cards;
            if (cards.Count <= 1)
                continue;

            List<NetFullCombatState.CardState> sorted = cards
                .Select((c, idx) => (c, idx))
                .OrderBy(t => CardStateSortKey(t.c))
                .ThenBy(t => t.idx)
                .Select(t => t.c)
                .ToList();
            pile.cards = sorted;
            piles[pi] = pile;
        }
    }

    /// <summary>
    /// Must reflect every field that <see cref="NetFullCombatState.CardState.Serialize"/> emits; vanilla only used
    /// Id/upgrade/floor from <see cref="SerializableCard"/>, but the packet also includes <see cref="SerializableCard.Enchantment"/>
    /// and <see cref="SerializableCard.Props"/> (and nested cards inside props). Identical partial keys with different
    /// props/enchantments produced different checksum bytes while our sort order stayed tied on Id—fix: full card fingerprint.
    /// </summary>
    private static string CardStateSortKey(NetFullCombatState.CardState cs)
    {
        SerializableCard? c = cs.card;
        string kw =
            cs.keywords is not { Count: > 0 }
                ? ""
                : string.Join(",", cs.keywords.OrderBy(k => (int)k).Select(k => ((int)k).ToString()));
        string cardFp = SerializableCardSortFingerprint(c, 0);
        return
            $"{cardFp}\u001f{cs.affliction?.Entry ?? ""}\u001f{cs.afflictionCount}\u001f{kw}";
    }

    private const int MaxCardFingerprintDepth = 32;

    private static string SerializableCardSortFingerprint(SerializableCard? c, int depth)
    {
        if (c == null)
            return "";
        if (depth > MaxCardFingerprintDepth)
            return "<depth>";

        string enchant = SerializableEnchantmentSortFingerprint(c.Enchantment, depth + 1);
        string props = SavedPropertiesSortFingerprint(c.Props, depth + 1);
        string floor = c.FloorAddedToDeck.HasValue ? c.FloorAddedToDeck.Value.ToString() : "";
        return $"{c.Id?.Entry ?? ""}\u001f{c.CurrentUpgradeLevel}\u001f{enchant}\u001f{props}\u001f{floor}";
    }

    private static string SerializableEnchantmentSortFingerprint(SerializableEnchantment? e, int depth)
    {
        if (e == null)
            return "";
        if (depth > MaxCardFingerprintDepth)
            return "<depth>";

        string props = SavedPropertiesSortFingerprint(e.Props, depth + 1);
        return $"{e.Id?.Entry ?? ""}\u001f{e.Amount}\u001f{props}";
    }

    /// <summary>
    /// Canonical key for <see cref="SavedProperties"/> independent of list construction order (vanilla serialize order
    /// is fixed, but reflection-built props can differ in list ordering across peers).
    /// </summary>
    private static string SavedPropertiesSortFingerprint(SavedProperties? p, int depth)
    {
        if (p == null)
            return "";
        if (depth > MaxCardFingerprintDepth)
            return "<depth>";

        var lines = new List<string>();

        if (p.ints != null)
        {
            foreach (var x in p.ints)
                lines.Add($"i\u001f{x.name}\u001f{x.value}");
        }

        if (p.intArrays != null)
        {
            foreach (var x in p.intArrays)
                lines.Add($"ia\u001f{x.name}\u001f{string.Join(",", x.value.Select(v => v.ToString()))}");
        }

        if (p.bools != null)
        {
            foreach (var x in p.bools)
                lines.Add($"b\u001f{x.name}\u001f{x.value}");
        }

        if (p.modelIds != null)
        {
            foreach (var x in p.modelIds)
                lines.Add($"m\u001f{x.name}\u001f{x.value.Entry}");
        }

        if (p.cards != null)
        {
            foreach (var x in p.cards)
                lines.Add($"c\u001f{x.name}\u001f{SerializableCardSortFingerprint(x.value, depth + 1)}");
        }

        if (p.cardArrays != null)
        {
            foreach (var x in p.cardArrays)
            {
                string[] nested = x.value.Select(sc => SerializableCardSortFingerprint(sc, depth + 1)).ToArray();
                lines.Add($"ca\u001f{x.name}\u001f{string.Join("\u0003", nested)}");
            }
        }

        if (p.strings != null)
        {
            foreach (var x in p.strings)
                lines.Add($"s\u001f{x.name}\u001f{EscapeFingerprintSegment(x.value)}");
        }

        if (lines.Count == 0)
            return "";

        lines.Sort(StringComparer.Ordinal);
        return string.Join("\u0002", lines);
    }

    private static string EscapeFingerprintSegment(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Replace("\u0001", "\\1").Replace("\u0002", "\\2").Replace("\u0003", "\\3");
    }

    /// <summary>
    /// Face-down overlay keyword on monsters/traps/spells (see <see cref="AbstractMonsterCard"/>). Snapshot copies
    /// <see cref="CardModel.Keywords"/> into <see cref="NetFullCombatState.CardState.keywords"/>; observers or
    /// immutable card instances can skip <see cref="AbstractMonsterCard.UpdateFaceDownKeywordFromBool"/>, leaving 10012
    /// on one peer but not the other. Strip from checksum bytes only — gameplay uses <see cref="AbstractMonsterCard.FaceDown"/>.
    /// </summary>
    private static readonly CardKeyword FaceDownPresentationKeywordChecksum = (CardKeyword)10012;

    private static void StripEphemeralYgoPresentationKeywordsFromChecksumPiles(ref NetFullCombatState.PlayerState ps)
    {
        List<NetFullCombatState.CombatPileState> piles = ps.piles;
        for (int pi = 0; pi < piles.Count; pi++)
        {
            NetFullCombatState.CombatPileState pile = piles[pi];
            List<NetFullCombatState.CardState> cards = pile.cards;
            for (int ci = 0; ci < cards.Count; ci++)
            {
                NetFullCombatState.CardState cs = cards[ci];
                if (cs.keywords is not { Count: > 0 })
                    continue;

                List<CardKeyword> filtered = cs.keywords.Where(k => k != FaceDownPresentationKeywordChecksum).ToList();
                if (filtered.Count == cs.keywords.Count)
                    continue;

                cs.keywords = filtered.Count == 0 ? null : filtered;
                cards[ci] = cs;
            }

            pile.cards = cards;
            piles[pi] = pile;
        }
    }

    private static string CreatureSortKeyForLog(NetFullCombatState.CreatureState c)
    {
        string id = c.monsterId?.Entry ?? "";
        string pid = c.playerId?.ToString() ?? "";
        return $"{id}\u001f{pid}";
    }

    private static void StabilizeKeywordOrderInPiles(ref NetFullCombatState.PlayerState ps)
    {
        List<NetFullCombatState.CombatPileState> piles = ps.piles;
        for (int pi = 0; pi < piles.Count; pi++)
        {
            NetFullCombatState.CombatPileState pile = piles[pi];
            List<NetFullCombatState.CardState> cards = pile.cards;
            for (int ci = 0; ci < cards.Count; ci++)
            {
                NetFullCombatState.CardState cs = cards[ci];
                if (cs.keywords is { Count: > 1 })
                {
                    cs.keywords = cs.keywords.OrderBy(k => (int)k).ToList();
                    cards[ci] = cs;
                }
            }

            pile.cards = cards;
            piles[pi] = pile;
        }
    }

    private static void StabilizeCreaturesForChecksum(NetFullCombatState state)
    {
        List<NetFullCombatState.CreatureState> creatures = state.Creatures;
        for (int i = 0; i < creatures.Count; i++)
        {
            NetFullCombatState.CreatureState c = creatures[i];
            if (c.powers is { Count: > 1 })
            {
                c.powers = c.powers.OrderBy(p => p.id.Entry).ThenBy(p => p.amount).ToList();
                creatures[i] = c;
            }
        }

        if (creatures.Count > 1)
            creatures.Sort((a, b) => string.CompareOrdinal(CreatureSortKey(a), CreatureSortKey(b)));
    }

    private static string CreatureSortKey(NetFullCombatState.CreatureState c)
    {
        string powers = string.Join(
            ",",
            (c.powers ?? []).Select(p => $"{p.id.Entry}\u001f{p.amount}"));
        return
            $"{c.monsterId?.Entry ?? ""}\u001f{c.playerId?.ToString() ?? ""}\u001f{c.currentHp}\u001f{c.maxHp}\u001f{c.block}\u001f{powers}";
    }

    /// <summary>
    /// Host detected checksum != client for the same id — print YGO pile fingerprints from the host snapshot.
    /// </summary>
    internal static void LogYgoFingerprintsOnHostCompareMismatch(
        IRunState? runState,
        NetFullCombatState? hostSnapshot,
        ulong remoteClientId,
        uint checksumId,
        uint hostHash,
        uint clientHash,
        string? context)
    {
        if (runState == null || hostSnapshot == null)
            return;
        if (RunManager.Instance?.NetService.Type != NetGameType.Host)
            return;

        GD.PrintErr(
            $"[YgoDuelist][MP][Divergence][HOST] remoteClient={remoteClientId} checksumId={checksumId} hostHash={hostHash} clientHash={clientHash} context={context}");
        PrintYgoDuelistFingerprintsFromSnapshot(runState, hostSnapshot, "[Divergence][HOST]");
    }

    /// <summary>
    /// Client received <see cref="ChecksumTracker"/> divergence — print LOCAL vs packet REMOTE YGO fingerprints for the Duelist.
    /// </summary>
    internal static void LogYgoFingerprintsOnClientDivergenceMessage(
        IRunState? runState,
        NetFullCombatState? localSnapshot,
        NetFullCombatState? remoteSnapshot,
        ulong peerNetId,
        uint checksumId,
        uint localHash,
        uint remoteHash,
        string? context)
    {
        if (runState == null)
            return;
        if (RunManager.Instance?.NetService.Type != NetGameType.Client)
            return;

        GD.PrintErr(
            $"[YgoDuelist][MP][Divergence][CLIENT] peer={peerNetId} checksumId={checksumId} localHash={localHash} remoteHash={remoteHash} context={context}");
        GD.PrintErr("[YgoDuelist][MP][Divergence][CLIENT] --- LOCAL snapshot (this peer) YGO fp ---");
        if (localSnapshot != null)
            PrintYgoDuelistFingerprintsFromSnapshot(runState, localSnapshot, "[Divergence][CLIENT][local]");
        GD.PrintErr("[YgoDuelist][MP][Divergence][CLIENT] --- REMOTE snapshot (packet) YGO fp ---");
        if (remoteSnapshot != null)
            PrintYgoDuelistFingerprintsFromSnapshot(runState, remoteSnapshot, "[Divergence][CLIENT][remote]");
    }

    private static void PrintYgoDuelistFingerprintsFromSnapshot(
        IRunState runState,
        NetFullCombatState snapshot,
        string tag)
    {
        bool any = false;
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Character is not YgoChar)
                continue;
            if (!TryFindPlayerState(snapshot, player.NetId, out NetFullCombatState.PlayerState ps))
                continue;
            any = true;
            GD.PrintErr($"[YgoDuelist][MP]{tag} {BuildYgoPlayerPilesFingerprint(ps)}");
        }

        if (!any)
            GD.PrintErr($"[YgoDuelist][MP]{tag} (no YgoDuelist in run — no YGO fp)");
    }

    private static bool TryFindPlayerState(NetFullCombatState snapshot, ulong netId, out NetFullCombatState.PlayerState ps)
    {
        foreach (NetFullCombatState.PlayerState p in snapshot.Players)
        {
            if (p.playerId == netId)
            {
                ps = p;
                return true;
            }
        }

        ps = default;
        return false;
    }
}
