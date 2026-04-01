using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.RestSite;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRestSite;

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
public static class YgoRestSiteSynchronizerBeginChargesPatch
{
    [HarmonyPostfix]
    public static void Postfix(RestSiteSynchronizer __instance)
    {
        IPlayerCollection coll = Traverse.Create(__instance).Field<IPlayerCollection>("_playerCollection").Value;
        foreach (Player p in coll.Players)
            YgoCampfireDeckEditCharges.ResetForRestVisit(p);
    }
}

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class YgoRestSiteOptionGenerateDeckRevampPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        __result.Add(new YgoDeckRevampRestSiteOption(player));
    }
}

/// <summary>
/// Base <see cref="RestSiteOption.Icon"/> builds a vanilla path from <see cref="RestSiteOption.OptionId"/>; that file does not exist for our option.
/// Preload and <see cref="NRestSiteButton"/> still call the getter before our Reload postfix can fix the widget.
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Icon), MethodType.Getter)]
public static class YgoDeckRevampRestSiteOptionIconPatch
{
    [HarmonyPrefix]
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is not YgoDeckRevampRestSiteOption)
            return true;
        Texture2D? tex = YgoDeckRevampRestSiteOption.LoadIconTexture();
        if (tex == null)
            return true;
        __result = tex;
        return false;
    }
}

[HarmonyPatch(typeof(NRestSiteButton), "Reload")]
public static class NRestSiteButtonYgoDeckRevampIconPatch
{
    [HarmonyPostfix]
    public static void Postfix(NRestSiteButton __instance)
    {
        if (__instance.Option is not YgoDeckRevampRestSiteOption)
            return;

        TextureRect? icon = __instance.GetNodeOrNull<TextureRect>("%Icon");
        if (icon == null)
            return;

        Texture2D? tex = YgoDeckRevampRestSiteOption.LoadIconTexture();
        if (tex != null)
            icon.Texture = tex;
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "UpdateRestSiteOptions")]
public static class NRestSiteRoomYgoCampfireDeckEditRowPatch
{
    private const string RowName = "YgoCampfireDeckEditRow";

    [HarmonyPostfix]
    public static void Postfix(NRestSiteRoom __instance)
    {
        Control? choices = Traverse.Create(__instance).Field<Control>("_choicesContainer").Value;
        if (choices == null || !choices.IsInsideTree())
            return;

        IRunState runState = Traverse.Create(__instance).Field<IRunState>("_runState").Value;
        Player? me = LocalContext.GetMe(runState);
        if (me == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(me))
            return;

        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(me);
        int remaining = YgoCampfireDeckEditCharges.GetRemaining(me);

        var row = new VBoxContainer { Name = RowName };
        choices.AddChild(row);

        var label = new Label();
        LocString rowLoc = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_ROW.label");
        rowLoc.AddObj("Count", remaining);
        label.Text = rowLoc.GetFormattedText();
        row.AddChild(label);

        var buttons = new HBoxContainer();
        row.AddChild(buttons);

        static void Wire(Button b, Player player, Func<Player, Task> flow)
        {
            b.Pressed += () =>
            {
                TaskHelper.RunSafely(RunFlowThenRefresh(player, flow));
            };
        }

        Button replace = new Button();
        replace.Text = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_ROW.replace").GetFormattedText();
        replace.Disabled = remaining <= 0;
        Wire(replace, me, YgoCampfireDeckEditService.RunReplaceAsync);
        buttons.AddChild(replace);

        Button add = new Button();
        add.Text = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_ROW.add").GetFormattedText();
        add.Disabled = remaining <= 0;
        Wire(add, me, YgoCampfireDeckEditService.RunAddAsync);
        buttons.AddChild(add);

        Button remove = new Button();
        remove.Text = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_ROW.remove").GetFormattedText();
        remove.Disabled = remaining <= 0;
        Wire(remove, me, YgoCampfireDeckEditService.RunRemoveAsync);
        buttons.AddChild(remove);
    }

    private static async Task RunFlowThenRefresh(Player player, Func<Player, Task> flow)
    {
        await flow(player);
        NRestSiteRoom? room = NRestSiteRoom.Instance;
        if (room != null)
            AccessTools.Method(typeof(NRestSiteRoom), "UpdateRestSiteOptions").Invoke(room, null);
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom.DefaultFocusedControl), MethodType.Getter)]
public static class NRestSiteRoomDefaultFocusedControlSafePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRestSiteRoom __instance, ref Control? __result)
    {
        Control? choices = Traverse.Create(__instance).Field<Control>("_choicesContainer").Value;
        if (choices == null)
        {
            __result = null;
            return false;
        }

        foreach (Node child in choices.GetChildren())
        {
            if (child is NRestSiteButton button)
            {
                __result = button;
                return false;
            }
        }

        __result = null;
        return false;
    }
}
