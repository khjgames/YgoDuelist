using System;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Extra <see cref="YgoCardPackTags.MultiplayerSafe"/> on <see cref="YgoPackCardCatalog.GetEffectivePackTags"/> for cards that
/// are multiplayer-safe but did not yet add the flag on each template (Draw themes, Elemental folder, monarchs, trap monsters,
/// <see cref="IDoubleTributeMaterial"/>, true normals with pack tier multiplier at least 1.1, etc.).
/// </summary>
internal static class YgoMultiplayerSafePackRules
{
    internal static bool ShouldAugmentMultiplayerSafe(YgoDuelistCard y, Type t)
    {
        if ((y.PackTags & YgoCardPackTags.MultiplayerSafe) != 0)
            return false;

        string full = t.FullName ?? string.Empty;

        if (YgoMultiplayerSafeExplicitPicks.FullNames.Contains(full))
            return true;

        if ((y.PackTags & YgoCardPackTags.Draw) != 0)
            return true;

        if (full.Contains(".Monster.Elemental.", StringComparison.Ordinal))
            return true;

        if (full.Contains(".TrapMonster.", StringComparison.Ordinal))
            return true;

        if (t.Name.Contains("Gravekeeper", StringComparison.Ordinal))
            return true;

        if (typeof(IDoubleTributeMaterial).IsAssignableFrom(t))
            return true;

        if (y is NormalMonsterCard nm && nm.YgoCardType == YgoCardType.Monster && nm.PackWeightMultiplier >= 1.1f)
            return true;

        return t.Name.Contains("Monarch", StringComparison.Ordinal);
    }
}
