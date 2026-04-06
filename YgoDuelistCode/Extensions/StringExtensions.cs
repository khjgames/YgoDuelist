namespace YgoDuelist.YgoDuelistCode.Extensions;

// Mostly utilities to get asset paths. Godot ResourceLoader / PreloadManager expect a res:// URI (same pattern as base ImageHelper).
public static class StringExtensions
{
    private static string ResPathFromJoined(string joined)
    {
        string n = joined.Replace('\\', '/');
        return n.StartsWith("res://", StringComparison.Ordinal) ? n : "res://" + n;
    }

    public static string ImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", path));
    }

    public static string CardImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "card_portraits", path));
    }

    public static string BigCardImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "card_portraits", "big", path));
    }

    public static string PowerImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "powers", path));
    }

    public static string BigPowerImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "powers", "big", path));
    }

    public static string RelicImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "relics", path));
    }

    public static string BigRelicImagePath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "relics", "big", path));
    }

    public static string CharacterUiPath(this string path)
    {
        return ResPathFromJoined(Path.Join(MainFile.ModId, "images", "charui", path));
    }
}
