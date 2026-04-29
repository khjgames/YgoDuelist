using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Picks a token card portrait variant (1..N) favoring indices not already used by the same token type on the field.
/// </summary>
public static class YgoTokenAlternatePortrait
{
    /// <summary>Builds <c>token_portraits/{stem}.png</c> or <c>token_portraits/{stem}_{n}.png</c> for <paramref name="index1Based"/> ≥ 2.</summary>
    public static string BuildPortraitPath(string stem, int index1Based)
    {
        string file = index1Based <= 1 ? $"{stem}.png" : $"{stem}_{index1Based}.png";
        return $"token_portraits/{file}".CardImagePath();
    }

    /// <summary>Assigns <see cref="YgoTokenNormalMonster"/> / <see cref="YgoTokenEffectMonster"/> portrait before summon; no-op for single-art tokens.</summary>
    public static void AssignForSummon(BaseMonsterCard card, Player player)
    {
        if (card is not IYgoTokenMonster tok || tok.TokenAlternatePortraitCount <= 1)
            return;

        int count = tok.TokenAlternatePortraitCount;
        Type tokenType = card.GetType();
        var used = new HashSet<int>();
        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (field.GetType() != tokenType)
                continue;
            int? idx = TryGetVariant(field);
            if (idx.HasValue)
                used.Add(idx.Value);
        }

        var available = new List<int>(count);
        for (int i = 1; i <= count; i++)
        {
            if (!used.Contains(i))
                available.Add(i);
        }

        int pick = available.Count > 0
            ? available[Random.Shared.Next(available.Count)]
            : Random.Shared.Next(1, count + 1);

        string stem = card.Id.Entry.RemovePrefix().ToLowerInvariant();
        string path = BuildPortraitPath(stem, pick);
        ApplyVariant(card, pick, path);
    }

    private static void ApplyVariant(BaseMonsterCard card, int index1Based, string path)
    {
        switch (card)
        {
            case YgoTokenNormalMonster n:
                n.ApplyTokenPortraitVariant(index1Based, path);
                break;
            case YgoTokenEffectMonster e:
                e.ApplyTokenPortraitVariant(index1Based, path);
                break;
        }
    }

    private static int? TryGetVariant(BaseMonsterCard field) =>
        field switch
        {
            YgoTokenNormalMonster n => n.TokenPortraitVariantIndex,
            YgoTokenEffectMonster e => e.TokenPortraitVariantIndex,
            _ => null
        };
}
