using System;
using System.IO;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

public abstract class YgoDuelistPower : CustomPowerModel
{
    /// <summary>
    /// MP: invoked for every live <see cref="Creature"/> before <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetFullCombatState"/>
    /// checksum snapshots. Override when stacks must match authoritative card/zone state (trap left play, field spell, etc.).
    /// Use <see cref="MegaCrit.Sts2.Core.Models.PowerModel.RemoveInternal"/> / <see cref="MegaCrit.Sts2.Core.Models.PowerModel.ApplyInternal"/> only — never <c>await</c> <see cref="MegaCrit.Sts2.Core.Commands.PowerCmd"/> here.
    /// </summary>
    public virtual void ReconcileForMpChecksumSnapshot(Creature owner)
    {
    }

    public override string CustomPackedIconPath => ResolveIconPath(big: false);

    public override string CustomBigIconPath => ResolveIconPath(big: true);

    /// <summary>
    /// When non-null and the dedicated <c>images/powers/&lt;powerId&gt;.png</c> is missing, use
    /// <c>images/card_portraits/{value}.png</c> for the icon. Default: derive a stem from the power id
    /// (<c>_power</c> / <c>_field_power</c> / etc.). Override only when that derivation does not match any card portrait.
    /// </summary>
    protected virtual string? CardPortraitStemOverride => null;

    private string ResolveIconPath(bool big)
    {
        string fileName = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";
        string powerPath = big ? fileName.BigPowerImagePath() : fileName.PowerImagePath();
        if (ResourceExists(powerPath))
            return powerPath;

        string powerStem = Path.GetFileNameWithoutExtension(fileName);
        string? portraitStem = CardPortraitStemOverride ?? DeriveCardPortraitStem(powerStem);
        string? fromPortrait = TryCardPortraitForStem(portraitStem, big);
        if (fromPortrait != null)
            return fromPortrait;

        return GetFallbackPowerIconPath(big);
    }

    private static string? TryCardPortraitForStem(string? portraitStem, bool big)
    {
        if (string.IsNullOrEmpty(portraitStem))
            return null;

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

        return null;
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

    private static string GetFallbackPowerIconPath(bool big)
    {
        string stiff = big ? "stiff_power.png".BigPowerImagePath() : "stiff_power.png".PowerImagePath();
        if (ResourceExists(stiff))
            return stiff;
        return big ? "power.png".BigPowerImagePath() : "power.png".PowerImagePath();
    }

    private static bool ResourceExists(string relativePath)
    {
        string p = relativePath.Replace('\\', '/');
        if (!p.StartsWith("res://", StringComparison.Ordinal))
            p = "res://" + p;
        return ResourceLoader.Exists(p);
    }

    /// <summary>
    /// Loads every shipped texture under <c>images/powers/</c> and <c>images/powers/big/</c> so first combat use does not hit cold-load stalls.
    /// </summary>
    public static void PreloadShippedPowerTextures()
    {
        PreloadPowerDirectory($"res://{MainFile.ModId}/images/powers");
        PreloadPowerDirectory($"res://{MainFile.ModId}/images/powers/big");
    }

    private static void PreloadPowerDirectory(string resDir)
    {
        using DirAccess? dir = DirAccess.Open(resDir);
        if (dir == null)
            return;

        dir.ListDirBegin();
        while (true)
        {
            string name = dir.GetNext();
            if (string.IsNullOrEmpty(name))
                break;
            if (dir.CurrentIsDir())
                continue;
            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            string path = $"{resDir.TrimEnd('/')}/{name}";
            if (!ResourceLoader.Exists(path))
                continue;
            _ = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        }

        dir.ListDirEnd();
    }
}
