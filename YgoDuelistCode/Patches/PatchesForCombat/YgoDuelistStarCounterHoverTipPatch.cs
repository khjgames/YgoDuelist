using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelistCharacter = global::YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Duelist: star counter uses conduit art, hides rotating star layers, conduit hover text.
/// Game update: <see cref="NCombatUi.Activate"/> reparents the star counter under <see cref="NEnergyCounter"/> after
/// <see cref="NStarCounter.Initialize"/>; we re-apply skin there so visuals stay correct. <see cref="NStarCounter._Ready"/>
/// still owns vanilla <c>_hoverTip</c> until first hover — we overwrite <c>_hoverTip</c> via reflection.
/// </summary>
[HarmonyPatch(typeof(NStarCounter), nameof(NStarCounter.Initialize))]
public static class YgoDuelistStarCounterHoverTipPatch
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private const string EnergyConduitTexturePath = "res://YgoDuelist/images/card_frames/energy_conduit.png";

    private static Texture2D? _energyConduitTexture;
    private static System.Reflection.FieldInfo? _hoverTipField;

    [HarmonyPostfix]
    public static void Postfix(NStarCounter __instance, Player player)
    {
        Log("[YgoDuelist StarCounter] Initialize postfix");
        ApplyConduitToStarCounter(__instance, player, "Initialize");
    }

    private static System.Reflection.FieldInfo? ResolveHoverTipField()
    {
        return _hoverTipField ??= AccessTools.Field(typeof(NStarCounter), "_hoverTip");
    }

    /// <summary>Re-apply after <c>NCombatUi</c> reparents the star counter under the energy counter.</summary>
    [HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
    public static class AfterCombatUiActivate
    {
        [HarmonyPostfix]
        public static void Postfix(NCombatUi __instance, CombatState state)
        {
            Log("[YgoDuelist StarCounter] NCombatUi.Activate postfix (after reparent)");
            var me = LocalContext.GetMe(state);
            var starCounter = __instance.GetNodeOrNull<NStarCounter>("%StarCounter");
            if (starCounter == null)
            {
                Log("[YgoDuelist StarCounter] ERROR: %StarCounter not found under NCombatUi after Activate");
                return;
            }

            ApplyConduitToStarCounter(starCounter, me, "Activate");
        }
    }

    internal static void ApplyConduitToStarCounter(NStarCounter starCounter, Player? player, string phase)
    {
        if (player?.Character is not YgoDuelistCharacter)
        {
            Log($"[YgoDuelist StarCounter] skip ({phase}): not YgoDuelist (character={(player?.Character == null ? "null" : player.Character.GetType().Name)})");
            return;
        }

        _energyConduitTexture ??= ResourceLoader.Load<Texture2D>(EnergyConduitTexturePath, null, ResourceLoader.CacheMode.Reuse);
        if (_energyConduitTexture == null)
        {
            Log($"[YgoDuelist StarCounter] WARN ({phase}): failed to load texture {EnergyConduitTexturePath}");
        }
        else
        {
            var icon = starCounter.GetNodeOrNull<TextureRect>("Icon");
            if (icon == null)
            {
                Log($"[YgoDuelist StarCounter] WARN ({phase}): child Icon not found or not TextureRect");
            }
            else
            {
                icon.Texture = _energyConduitTexture;
                Log($"[YgoDuelist StarCounter] OK ({phase}): Icon.Texture set to conduit");
            }

            var rotationLayers = starCounter.GetNodeOrNull<Control>("%RotationLayers");
            if (rotationLayers == null)
                Log($"[YgoDuelist StarCounter] WARN ({phase}): %RotationLayers not found");
            else
            {
                rotationLayers.Visible = false;
                Log($"[YgoDuelist StarCounter] OK ({phase}): RotationLayers hidden");
            }
        }

        var title = new LocString("static_hover_tips", "CONDUIT_COUNT.title");
        var description = new LocString("static_hover_tips", "CONDUIT_COUNT.description");
        description.Add("conduitIcon", ConduitImgBbcode);

        var tip = new HoverTip(title, description);
        var field = ResolveHoverTipField();
        if (field == null)
        {
            Log($"[YgoDuelist StarCounter] ERROR ({phase}): NStarCounter._hoverTip field not found (game update?)");
            return;
        }

        field.SetValue(starCounter, tip);
        Log($"[YgoDuelist StarCounter] OK ({phase}): _hoverTip replaced with CONduit_COUNT");
    }

    private static void Log(string message)
    {
        GD.Print(message);
        MainFile.Logger.Info(message);
    }
}
