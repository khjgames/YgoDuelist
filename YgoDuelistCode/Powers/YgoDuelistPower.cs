using System;
using System.IO;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

public abstract class YgoDuelistPower : CustomPowerModel
{
    public override string CustomPackedIconPath => ResolveIconPath(big: false);

    public override string CustomBigIconPath => ResolveIconPath(big: true);

    private string ResolveIconPath(bool big)
    {
        string fileName = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";
        return ResolvePowerOrCardPortrait(fileName, big);
    }

    /// <summary>
    /// Prefer <c>images/powers/</c>; if missing, use the matching card portrait (e.g. <c>*_field_power</c> → card stem).
    /// </summary>
    protected static string ResolvePowerOrCardPortrait(string powerFileName, bool big)
    {
        string powerPath = big ? powerFileName.BigPowerImagePath() : powerFileName.PowerImagePath();
        if (ResourceExists(powerPath))
            return powerPath;

        string powerStem = Path.GetFileNameWithoutExtension(powerFileName);
        string? portraitStem = DeriveCardPortraitStem(powerStem);
        if (portraitStem == null)
            return powerPath;

        string portraitFile = $"{portraitStem}.png";
        if (big)
        {
            string bigPortrait = portraitFile.BigCardImagePath();
            if (ResourceExists(bigPortrait))
                return bigPortrait;
            string packedPortrait = portraitFile.CardImagePath();
            if (ResourceExists(packedPortrait))
                return packedPortrait;
        }
        else
        {
            string packedPortrait = portraitFile.CardImagePath();
            if (ResourceExists(packedPortrait))
                return packedPortrait;
        }

        return powerPath;
    }

    private static string? DeriveCardPortraitStem(string powerStem)
    {
        if (powerStem.EndsWith("_field_power_plus", StringComparison.Ordinal))
            return powerStem[..^"_field_power_plus".Length];
        if (powerStem.EndsWith("_field_power", StringComparison.Ordinal))
            return powerStem[..^"_field_power".Length];
        if (powerStem.EndsWith("_power_plus", StringComparison.Ordinal))
            return powerStem[..^"_power_plus".Length];
        if (powerStem.EndsWith("_power", StringComparison.Ordinal))
            return powerStem[..^"_power".Length];
        return null;
    }

    private static bool ResourceExists(string relativePath)
    {
        string p = relativePath.Replace('\\', '/');
        if (!p.StartsWith("res://", StringComparison.Ordinal))
            p = "res://" + p;
        return ResourceLoader.Exists(p);
    }
}
