using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Extensions;
using Godot;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Vanilla resolves outline to <c>images/ui/top_panel/character_icon_{id}_outline.png</c>, which is not shipped under the mod's
/// custom icon path layout. Reuse the same char UI asset as the main icon so map vote UI loads.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
public static class YgoDuelistIconOutlineTexturePatch
{
    public static bool Prefix(CharacterModel __instance, ref Texture2D? __result)
    {
        if (__instance is not YgoChar)
            return true;

        __result = PreloadManager.Cache.GetTexture2D("character_icon_char_name.png".CharacterUiPath());
        return false;
    }
}
