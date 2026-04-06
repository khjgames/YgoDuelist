namespace YgoDuelist.YgoDuelistCode.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", path).Replace('\\', '/');
    }

    public static string CardImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "card_portraits", path).Replace('\\', '/');
    }

    public static string BigCardImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "card_portraits", "big", path).Replace('\\', '/');
    }

    public static string PowerImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "powers", path).Replace('\\', '/');
    }

    public static string BigPowerImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "powers", "big", path).Replace('\\', '/');
    }

    public static string RelicImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "relics", path).Replace('\\', '/');
    }

    public static string BigRelicImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "relics", "big", path).Replace('\\', '/');
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "charui", path).Replace('\\', '/');
    }
}