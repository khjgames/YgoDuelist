using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When playing as the Duelist: combat star counter uses <c>energy_conduit.png</c>, hides vanilla rotating star
/// layers, and swaps the hover tip to Conduit wording with the small conduit icon.
/// </summary>
[HarmonyPatch(typeof(NStarCounter), nameof(NStarCounter.Initialize))]
public static class YgoDuelistStarCounterHoverTipPatch
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private const string EnergyConduitTexturePath = "YgoDuelist/images/card_frames/energy_conduit.png";

    private static Texture2D? _energyConduitTexture;

    [HarmonyPostfix]
    public static void Postfix(NStarCounter __instance, Player player)
    {
        if (player?.Character is not global::YgoDuelist.YgoDuelistCode.Character.YgoDuelist)
            return;

        _energyConduitTexture ??= ResourceLoader.Load<Texture2D>(EnergyConduitTexturePath, null, ResourceLoader.CacheMode.Reuse);
        if (_energyConduitTexture != null)
        {
            var icon = __instance.GetNodeOrNull<TextureRect>("Icon");
            if (icon != null)
                icon.Texture = _energyConduitTexture;

            var rotationLayers = __instance.GetNodeOrNull<Control>("%RotationLayers");
            if (rotationLayers != null)
                rotationLayers.Visible = false;
        }

        var title = new LocString("static_hover_tips", "CONDUIT_COUNT.title");
        var description = new LocString("static_hover_tips", "CONDUIT_COUNT.description");
        description.Add("conduitIcon", ConduitImgBbcode);

        var tip = new HoverTip(title, description);
        Traverse.Create(__instance).Field("_hoverTip").SetValue(tip);
    }
}
