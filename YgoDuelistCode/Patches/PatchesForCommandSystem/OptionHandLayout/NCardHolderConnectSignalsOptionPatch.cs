using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Option holders are built in code (no scene); GetNode("%Hitbox") can fail if the
/// unique name isn't registered yet. This patch runs ConnectSignals for option holders
/// with a fallback: use GetNodeOrNull("%Hitbox") or the first NClickableControl child.
/// </summary>
[HarmonyPatch(typeof(NCardHolder), "ConnectSignals")]
public static class NCardHolderConnectSignalsOptionPatch
{
    static bool Prefix(NCardHolder __instance)
    {
        if (__instance is not NYgoOptionCardHolder)
            return true;

        NClickableControl? hitbox = __instance.GetNodeOrNull<NClickableControl>("%Hitbox");
        hitbox ??= __instance.GetChildren().OfType<NClickableControl>().FirstOrDefault();

        if (hitbox == null)
            return true; // let original run and throw

        if (__instance.CardNode != null)
            __instance.CardNode.Position = Vector2.Zero;

        __instance.Connect(Control.SignalName.FocusEntered, Callable.From(BindOnFocus(__instance)));
        __instance.Connect(Control.SignalName.FocusExited, Callable.From(BindOnUnfocus(__instance)));

        var hitboxField = AccessTools.Field(typeof(NCardHolder), "_hitbox");
        hitboxField?.SetValue(__instance, hitbox);

        var onFocus = AccessTools.Method(typeof(NCardHolder), "OnFocus");
        var onUnfocus = AccessTools.Method(typeof(NCardHolder), "OnUnfocus");
        var onMousePressed = AccessTools.Method(typeof(NCardHolder), "OnMousePressed");
        var onMouseReleased = AccessTools.Method(typeof(NCardHolder), "OnMouseReleased");
        var onChildExitingTree = AccessTools.Method(typeof(NCardHolder), "OnChildExitingTree");

        hitbox.Connect(NClickableControl.SignalName.Focused, Callable.From<NClickableControl>(_ => { onFocus?.Invoke(__instance, null); }));
        hitbox.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NClickableControl>(_ => { onUnfocus?.Invoke(__instance, null); }));
        hitbox.Connect(NClickableControl.SignalName.MousePressed, Callable.From<InputEvent>(e => { onMousePressed?.Invoke(__instance, new object[] { e }); }));
        hitbox.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(e => { onMouseReleased?.Invoke(__instance, new object[] { e }); }));
        __instance.Connect(Node.SignalName.ChildExitingTree, Callable.From<Node>(n => { onChildExitingTree?.Invoke(__instance, new object[] { n }); }));

        return false;
    }

    static System.Action BindOnFocus(NCardHolder h)
    {
        var m = AccessTools.Method(typeof(NCardHolder), "OnFocus");
        return () => m?.Invoke(h, null);
    }

    static System.Action BindOnUnfocus(NCardHolder h)
    {
        var m = AccessTools.Method(typeof(NCardHolder), "OnUnfocus");
        return () => m?.Invoke(h, null);
    }
}
