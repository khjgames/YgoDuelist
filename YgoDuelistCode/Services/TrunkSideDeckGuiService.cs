using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Toggleable shell for Trunk/Side pages 1–2; remembers last active tab index (0 = Side→Trunk, 1 = Trunk→Side).
/// </summary>
public static class TrunkSideDeckGuiService
{
    public const int TabSideToTrunk = 0;
    public const int TabTrunkToSide = 1;

    /// <summary>Last page used; persists while the run continues.</summary>
    public static int ActiveTab { get; set; }

    private static Window? _shell;

    public static bool IsShellOpen => _shell != null && GodotObject.IsInstanceValid(_shell);

    public static bool TryToggleCloseShell()
    {
        if (_shell == null || !GodotObject.IsInstanceValid(_shell))
        {
            _shell = null;
            return false;
        }

        _shell.QueueFree();
        _shell = null;
        return true;
    }

    public static void CloseShellIfOpen()
    {
        TryToggleCloseShell();
    }

    public static void OpenShell(Player player)
    {
        TryToggleCloseShell();
        YgoRelicBrowseGridOverlayPatch.CloseAnyActiveBrowseGrid();

        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
            return;

        var shellTitle = new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.shell_title");
        var win = new Window
        {
            Title = shellTitle.GetFormattedText(),
            Size = new Vector2I(440, 200),
            Unresizable = true,
            Exclusive = true
        };

        win.CloseRequested += () =>
        {
            win.QueueFree();
            if (ReferenceEquals(_shell, win))
                _shell = null;
        };

        var margin = new MarginContainer { OffsetLeft = 12, OffsetTop = 12, OffsetRight = -12, OffsetBottom = -12 };
        win.AddChild(margin);
        var vbox = new VBoxContainer();
        margin.AddChild(vbox);

        var hint = new Label
        {
            Text = new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.shell_hint").GetFormattedText(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        vbox.AddChild(hint);

        var tabLabel = new Label();
        vbox.AddChild(tabLabel);

        void RefreshTabLabel()
        {
            string key = ActiveTab == TabSideToTrunk
                ? "YGODUELIST-TRUNK_SIDE_DECK_RELIC.tab_side_to_trunk"
                : "YGODUELIST-TRUNK_SIDE_DECK_RELIC.tab_trunk_to_side";
            tabLabel.Text = new LocString("relics", key).GetFormattedText();
        }

        RefreshTabLabel();

        var btnSideToTrunk = new Button
        {
            Text = new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.button_side_to_trunk").GetFormattedText()
        };
        btnSideToTrunk.Pressed += () =>
        {
            ActiveTab = TabSideToTrunk;
            RefreshTabLabel();
            TaskHelper.RunSafely(RunSideToTrunkAsync(player));
        };
        vbox.AddChild(btnSideToTrunk);

        var btnTrunkToSide = new Button
        {
            Text = new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.button_trunk_to_side").GetFormattedText()
        };
        btnTrunkToSide.Pressed += () =>
        {
            ActiveTab = TabTrunkToSide;
            RefreshTabLabel();
            TaskHelper.RunSafely(RunTrunkToSideAsync(player));
        };
        vbox.AddChild(btnTrunkToSide);

        tree.Root.AddChild(win);
        win.PopupCentered();
        _shell = win;
    }

    private static async Task RunSideToTrunkAsync(Player player)
    {
        List<CardModel> cards = TrunkSideDeckRelic.GetSideDeckCards(player).ToList();
        int max = cards.Count;
        var prefs = new CardSelectorPrefs(
            new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.prompt_side_to_trunk"),
            0,
            max)
        {
            Cancelable = true
        };

        YgoRelicBrowseGridOverlayPatch.SetPendingKind(YgoRelicBrowseGridOverlayPatch.RelicGridKind.TrunkSideDeckSelect);
        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                cards,
                player,
                prefs);
        }
        finally
        {
            YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
        }

        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        foreach (CardModel c in picked)
        {
            if (!side.Cards.Contains(c))
                continue;
            side.RemoveInternal(c, silent: true);
            trunk.AddInternal(c, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
    }

    private static async Task RunTrunkToSideAsync(Player player)
    {
        List<CardModel> cards = TrunkSideDeckRelic.GetTrunkCards(player).ToList();
        int max = cards.Count;
        var prefs = new CardSelectorPrefs(
            new LocString("relics", "YGODUELIST-TRUNK_SIDE_DECK_RELIC.prompt_trunk_to_side"),
            0,
            max)
        {
            Cancelable = true
        };

        YgoRelicBrowseGridOverlayPatch.SetPendingKind(YgoRelicBrowseGridOverlayPatch.RelicGridKind.TrunkSideDeckSelect);
        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                cards,
                player,
                prefs);
        }
        finally
        {
            YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
        }

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        foreach (CardModel c in picked)
        {
            if (!trunk.Cards.Contains(c))
                continue;
            trunk.RemoveInternal(c, silent: true);
            side.AddInternal(c, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
    }
}
