using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.addons.mega_text;
using YgoDuelist.YgoDuelistCode.Rewards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Campfire deck edit overlay: vanilla loot <c>Rewards</c> frame and <c>RewardContainerMask</c> from <c>screens/rewards_screen</c>.
/// </summary>
public partial class NYgoCampfireDeckEditMenuScreen : Control, IOverlayScreen, IScreenContext
{
    private static readonly string RewardsScreenScenePath = SceneHelper.GetScenePath("screens/rewards_screen");

    private readonly Player _player;
    private YgoCampfireDeckEditUiReward? _storeReward;
    private YgoCampfireDeckEditUiReward? _putReward;
    private NRewardButton? _storeBtn;
    private NRewardButton? _putBtn;
    private NBackButton? _backButton;

    private MarginContainer? _rootMargin;
    private VBoxContainer? _mainVBox;
    private CenterContainer? _lootCenter;
    private Control? _lootStack;
    private Control? _maskHost;

    private NYgoCampfireDeckEditMenuScreen(Player player)
    {
        _player = player;
    }

    public static void Push(Player player)
    {
        var screen = new NYgoCampfireDeckEditMenuScreen(player);
        screen.Name = nameof(NYgoCampfireDeckEditMenuScreen);
        NOverlayStack.Instance!.Push(screen);
    }

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => (Control?)_storeBtn ?? this;

    public void AfterOverlayOpened()
    {
        YgoDeckEditCornerUi.SetDeckEditMenuOpen(true);
        Modulate = Colors.White;
    }

    public void AfterOverlayClosed()
    {
        YgoDeckEditCornerUi.SetDeckEditMenuOpen(false);
    }

    public void AfterOverlayShown()
    {
        Visible = true;
        _lootCenter?.QueueSort();

        Callable.From(() => { _lootCenter?.QueueSort(); }).CallDeferred();

        DumpDeckEditMenuLayout("AfterOverlayShown");
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Stop;
        Modulate = Colors.White;

        var margin = new MarginContainer
        {
            Name = "DeckEditMenuMargin",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 48);
        margin.AddThemeConstantOverride("margin_right", 48);
        margin.AddThemeConstantOverride("margin_top", (int)(48f + YgoCampfireDeckEditLayout.DeckEditMenuContentOffsetY));
        margin.AddThemeConstantOverride("margin_bottom", 120);
        AddChild(margin);
        _rootMargin = margin;

        var mainVBox = new VBoxContainer
        {
            Name = "DeckEditMainVBox",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        mainVBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddChild(mainVBox);
        _mainVBox = mainVBox;

        var lootCenter = new CenterContainer
        {
            Name = "DeckEditLootCenter",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        mainVBox.AddChild(lootCenter);
        _lootCenter = lootCenter;

        (Control stack, Control rewardsContainer, Control maskHost) = BuildLootFrameAndList();
        lootCenter.AddChild(stack);
        _lootStack = stack;
        _maskHost = maskHost;

        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(_player);

        _storeReward = new YgoCampfireDeckEditUiReward(
            _player,
            StoreRowLoc(YgoCampfireDeckEditCharges.GetStoreRemaining(_player)),
            async () =>
            {
                await YgoCampfireDeckEditService.RunStoreToTrunkAsync(_player);
                Callable.From(RefreshRows).CallDeferred();
            });

        _putReward = new YgoCampfireDeckEditUiReward(
            _player,
            PutRowLoc(YgoCampfireDeckEditCharges.GetPutInDeckRemaining(_player)),
            async () =>
            {
                await YgoCampfireDeckEditService.RunPutInDeckAsync(_player);
                Callable.From(RefreshRows).CallDeferred();
            });

        _storeBtn = NRewardButton.Create(_storeReward, null!);
        _storeBtn.Name = "DeckEditStoreRow";
        _storeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rewardsContainer.AddChild(_storeBtn);

        var rowGap = new Control
        {
            CustomMinimumSize = new Vector2(0f, 16f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        rewardsContainer.AddChild(rowGap);

        _putBtn = NRewardButton.Create(_putReward, null!);
        _putBtn.Name = "DeckEditPutRow";
        _putBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rewardsContainer.AddChild(_putBtn);

        AttachBackButton();
        RefreshRows();

        Callable.From(DeferredLootLayoutPass).CallDeferred();
        Callable.From(() => DumpDeckEditMenuLayout("_Ready deferred")).CallDeferred();
    }

    private void DeferredLootLayoutPass()
    {
        _lootCenter?.QueueSort();
    }

    private void DumpDeckEditMenuLayout(string tag)
    {
        if (!YgoCampfireDeckEditLayout.DebugLogDeckEditMenu)
            return;

        void Line(string label, Control? c)
        {
            if (c == null || !GodotObject.IsInstanceValid(c))
            {
                GD.PrintErr($"[YgoDeckEditMenu] {tag} {label}: null");
                return;
            }

            Rect2 g = c.GetGlobalRect();
            Control? p = c.GetParent() as Control;
            GD.PrintErr(
                $"[YgoDeckEditMenu] {tag} {label}: type={c.GetType().Name} visible={c.Visible} modulate={c.Modulate} " +
                $"clip={c.ClipContents} mouse={c.MouseFilter} z={c.ZIndex} layoutMode={c.LayoutMode} " +
                $"pos={c.Position} size={c.Size} min={c.CustomMinimumSize} globalRect={g} " +
                $"parent={(p == null ? "null" : $"{p.GetType().Name}:{p.Name}")}");
        }

        Line("rootMargin", _rootMargin);
        Line("mainVBox", _mainVBox);
        Line("lootCenter", _lootCenter);
        Line("lootStack", _lootStack);
        Line("maskHost", _maskHost);
        Line("storeBtn", _storeBtn);
        Line("putBtn", _putBtn);
        Line("backButton", _backButton);

        if (_lootStack != null && GodotObject.IsInstanceValid(_lootStack))
        {
            for (int i = 0; i < _lootStack.GetChildCount(); i++)
            {
                if (_lootStack.GetChild(i) is Control ch)
                    Line($"lootStack[{i}]_{ch.Name}", ch);
            }
        }

        if (_maskHost != null && GodotObject.IsInstanceValid(_maskHost))
        {
            Control? rc = _maskHost.FindChild("RewardsContainer", recursive: true, owned: false) as Control;
            Line("mask..RewardsContainer", rc);
        }
    }

    /// <summary>
    /// Outer <c>Rewards</c> art frame + <c>RewardContainerMask</c> list (same assets as combat loot).
    /// </summary>
    private static (Control Stack, Control RewardsContainer, Control MaskHost) BuildLootFrameAndList()
    {
        PackedScene scene = PreloadManager.Cache.GetScene(RewardsScreenScenePath);
        var inst = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);

        Control frameSrc = inst.GetNode<Control>("Rewards");
        Control maskSrc = inst.GetNode<Control>("%RewardContainerMask");

        float minW = YgoCampfireDeckEditLayout.DeckEditLootPanelMinWidth;
        float minH = YgoCampfireDeckEditLayout.DeckEditLootPanelMinHeight;

        var stack = new Control
        {
            Name = "DeckEditLootStack",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        stack.CustomMinimumSize = new Vector2(minW, minH);

        Control bg = (Control)frameSrc.Duplicate();
        bg.Name = "DeckEditRewardsFrame";
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        bg.Visible = true;
        bg.Modulate = Colors.White;
        stack.AddChild(bg);
        bg.LayoutMode = 1;
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        string deckEditTitle = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_EDIT.header").GetRawText();
        if (bg.GetNodeOrNull("Background/Banner/HeaderLabel") is Label lootHeader)
            lootHeader.Text = deckEditTitle;

        if (bg.GetNodeOrNull<Control>("%RewardContainerMask") is Control innerMask)
            innerMask.Visible = false;

        Control maskDup = (Control)maskSrc.Duplicate();
        maskDup.Name = "DeckEditRewardMask";
        maskDup.MouseFilter = Control.MouseFilterEnum.Stop;
        maskDup.Visible = true;
        maskDup.Modulate = Colors.White;
        stack.AddChild(maskDup);
        maskDup.LayoutMode = 1;
        maskDup.AnchorLeft = maskSrc.AnchorLeft;
        maskDup.AnchorTop = maskSrc.AnchorTop;
        maskDup.AnchorRight = maskSrc.AnchorRight;
        maskDup.AnchorBottom = maskSrc.AnchorBottom;
        maskDup.OffsetLeft = maskSrc.OffsetLeft;
        maskDup.OffsetTop = maskSrc.OffsetTop;
        maskDup.OffsetRight = maskSrc.OffsetRight;
        maskDup.OffsetBottom = maskSrc.OffsetBottom;
        maskDup.GrowHorizontal = maskSrc.GrowHorizontal;
        maskDup.GrowVertical = maskSrc.GrowVertical;

        Control? rewardsContainer = maskDup.GetNodeOrNull<Control>("%RewardsContainer")
            ?? maskDup.FindChild("RewardsContainer", recursive: true, owned: false) as Control;
        if (rewardsContainer == null)
            throw new InvalidOperationException(
                "NYgoCampfireDeckEditMenuScreen: rewards_screen has no RewardsContainer under RewardContainerMask.");

        rewardsContainer.Scale = Vector2.One;
        rewardsContainer.Visible = true;
        rewardsContainer.Modulate = Colors.White;

        inst.QueueFree();
        ClearChildren(rewardsContainer);
        HideScrollbarsRecursive(stack);
        return (stack, rewardsContainer, maskDup);
    }

    private static void ClearChildren(Node node)
    {
        while (node.GetChildCount() > 0)
        {
            Node ch = node.GetChild(0);
            node.RemoveChild(ch);
            ch.QueueFree();
        }
    }

    private static void HideScrollbarsRecursive(Node node)
    {
        if (node.GetType().Name == "NScrollbar" && node is Control c)
            c.Visible = false;
        foreach (Node child in node.GetChildren())
            HideScrollbarsRecursive(child);
    }

    private static LocString StoreRowLoc(int count)
    {
        var loc = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_EDIT.store_row");
        loc.AddObj("Num", count.ToString());
        return loc;
    }

    private static LocString StoreAtMinimumLoc() =>
        new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_EDIT.store_at_minimum");

    private static LocString PutRowLoc(int count)
    {
        var loc = new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_EDIT.put_row");
        loc.AddObj("Num", count.ToString());
        return loc;
    }

    private void RefreshRows()
    {
        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(_player);
        int storeN = YgoCampfireDeckEditCharges.GetStoreRemaining(_player);
        int putN = YgoCampfireDeckEditCharges.GetPutInDeckRemaining(_player);
        int minDeck = YgoPlayerMinimumDeck.Get(_player);
        bool deckAtOrBelowMinimum = _player.Deck.Cards.Count <= minDeck;

        if (deckAtOrBelowMinimum)
        {
            LocString atMin = StoreAtMinimumLoc();
            _storeReward?.SetDescription(atMin);
            if (_storeBtn != null)
            {
                MegaRichTextLabel? storeLab = _storeBtn.GetNodeOrNull<MegaRichTextLabel>("%Label");
                if (storeLab != null)
                    storeLab.Text = atMin.GetFormattedText();
            }

            _storeBtn?.Disable();
        }
        else
        {
            _storeReward?.SetDescription(StoreRowLoc(storeN));
            if (_storeBtn != null)
            {
                MegaRichTextLabel? storeLab = _storeBtn.GetNodeOrNull<MegaRichTextLabel>("%Label");
                if (storeLab != null && _storeReward != null)
                    storeLab.Text = _storeReward.Description.GetFormattedText();
            }

            if (storeN <= 0)
                _storeBtn?.Disable();
            else
                _storeBtn?.Enable();
        }

        _putReward?.SetDescription(PutRowLoc(putN));

        if (_putBtn != null)
        {
            MegaRichTextLabel? putLab = _putBtn.GetNodeOrNull<MegaRichTextLabel>("%Label");
            if (putLab != null && _putReward != null)
                putLab.Text = _putReward.Description.GetFormattedText();
        }

        if (putN <= 0)
            _putBtn?.Disable();
        else
            _putBtn?.Enable();
    }

    private void AttachBackButton()
    {
        PackedScene deckScene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/card_selection/deck_card_select_screen"));
        var deckSrc = deckScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        NBackButton? closeSrc = deckSrc.GetNodeOrNull<NBackButton>("%Close");
        if (closeSrc == null)
        {
            deckSrc.QueueFree();
            throw new InvalidOperationException("NYgoCampfireDeckEditMenuScreen: deck_card_select_screen must expose %Close.");
        }

        _backButton = (NBackButton)closeSrc.Duplicate();
        deckSrc.QueueFree();
        _backButton.Name = "DeckEditMenuClose";
        _backButton.Visible = true;
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => CloseMenu()));
        AddChild(_backButton);
        _backButton.Enable();
    }

    private void CloseMenu()
    {
        if (GodotObject.IsInstanceValid(this))
            NOverlayStack.Instance?.Remove(this);
    }
}
