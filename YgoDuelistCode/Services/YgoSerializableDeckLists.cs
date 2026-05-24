using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Splits <see cref="SerializablePlayer.Deck"/> save rows into main / extra / trunk / side for YgoDuelist.
/// Trailer layout matches <see cref="Patches.PlayerToSerializableAppendYgoExtraDeckPatch"/> and
/// <see cref="Patches.PlayerToSerializableAppendYgoTrunkSidePatch"/>.
/// </summary>
public static class YgoSerializableDeckLists
{
    private static ModelId? _ygoCharacterId;

    public static ModelId YgoCharacterId => _ygoCharacterId ??= ModelDb.Character<YgoChar>().Id;

    public static bool IsYgoCharacter(ModelId characterId) => characterId.Equals(YgoCharacterId);

    public readonly struct SplitResult
    {
        public List<SerializableCard> Main { get; init; }
        public List<SerializableCard> Extra { get; init; }
        public List<SerializableCard> Trunk { get; init; }
        public List<SerializableCard> Side { get; init; }
        public SerializableCard Marker { get; init; }
    }

    /// <summary>Non-mutating read of trailer bounds. Returns false when the list has no YGO marker trailer.</summary>
    public static bool TryReadSplit(IReadOnlyList<SerializableCard> deck, out SplitResult result)
    {
        result = default;
        if (deck.Count == 0)
            return false;

        ModelId markerId = ModelDb.Card<YgoSaveTrunkSideMarkerCard>().Id;
        SerializableCard marker = deck[^1];
        if (!YgoSaveTrunkSideMarkerCard.IsMarker(marker, markerId))
            return false;

        YgoSaveTrunkSideMarkerCard.ReadTrailerCounts(marker, out int extraCount, out int trunkCount, out int sideCount);
        int trailerLen = 1 + extraCount + trunkCount + sideCount;
        if (deck.Count < trailerLen)
            return false;

        int mainLen = deck.Count - trailerLen;
        int offset = mainLen;
        var extra = new List<SerializableCard>(extraCount);
        for (int i = 0; i < extraCount; i++)
            extra.Add(deck[offset++]);

        var trunk = new List<SerializableCard>(trunkCount);
        for (int i = 0; i < trunkCount; i++)
            trunk.Add(deck[offset++]);

        var side = new List<SerializableCard>(sideCount);
        for (int i = 0; i < sideCount; i++)
            side.Add(deck[offset++]);

        var main = new List<SerializableCard>(mainLen);
        for (int i = 0; i < mainLen; i++)
            main.Add(deck[i]);

        result = new SplitResult
        {
            Main = main,
            Extra = extra,
            Trunk = trunk,
            Side = side,
            Marker = marker
        };
        return true;
    }

    /// <summary>Main deck cards for badges / run history; returns the full list when not YGO or trailer is missing.</summary>
    public static IReadOnlyList<SerializableCard> MainDeckForCharacter(ModelId characterId, IEnumerable<SerializableCard> deck)
    {
        List<SerializableCard> list = deck as List<SerializableCard> ?? deck.ToList();
        if (!IsYgoCharacter(characterId))
            return list;
        return TryReadSplit(list, out SplitResult split) ? split.Main : list;
    }

    /// <summary>
    /// Mutates <paramref name="deck"/> in place: removes marker and trailing extra/trunk/side rows (same as save load).
    /// </summary>
    public static bool TryExtractTrailer(List<SerializableCard> deck, out SplitResult trailer)
    {
        trailer = default;
        if (!TryReadSplit(deck, out SplitResult split))
            return false;

        int remove = deck.Count - split.Main.Count;
        deck.RemoveRange(split.Main.Count, remove);
        trailer = split;
        return true;
    }
}
