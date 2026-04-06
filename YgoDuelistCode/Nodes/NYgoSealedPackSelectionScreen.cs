using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Pick one of three sealed packs, then confirm — banner centered, bottom bar Back + Confirm like card grid flows.
/// </summary>
public partial class NYgoSealedPackSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private IReadOnlyList<YgoCardPackTags> _masks = null!;

    private readonly List<NYgoSealedPackWidget> _widgets = new();
    private readonly TaskCompletionSource<int> _completion = new();

    private int _selectedIndex = -1;
    private NConfirmButton? _confirmButton;
    private NBackButton? _backButton;
    private HBoxContainer? _packRow;
    private VBoxContainer? _mainVBox;
    private HBoxContainer? _bottomBar;
    private MarginContainer? _rootMargin;
    private Tween? _fadeTween;

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _widgets.Count > 0 ? _widgets[0] : this;

    public static NYgoSealedPackSelectionScreen Push(IReadOnlyList<YgoCardPackTags> masks)
    {
        var screen = new NYgoSealedPackSelectionScreen();
        screen.Name = nameof(NYgoSealedPackSelectionScreen);
        screen._masks = masks;
        NOverlayStack.Instance!.Push(screen);
        return screen;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Stop;

        _rootMargin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Stop, Name = "SealedPackRootMargin" };
        _rootMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _rootMargin.AddThemeConstantOverride("margin_left", 40);
        _rootMargin.AddThemeConstantOverride("margin_right", 40);
        _rootMargin.AddThemeConstantOverride("margin_top", 32);
        _rootMargin.AddThemeConstantOverride("margin_bottom", 32);
        AddChild(_rootMargin);

        var vbox = new VBoxContainer
        {
            Name = "SealedPackVBox",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _rootMargin.AddChild(vbox);
        _mainVBox = vbox;

        var bannerCenter = new CenterContainer
        {
            Name = "SealedPackBannerCenter",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        vbox.AddChild(bannerCenter);

        NCommonBanner banner = DuplicateBanner();
        banner.Name = "SealedPackBanner";
        banner.LayoutMode = 2;
        banner.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        banner.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        banner.Visible = true;
        bannerCenter.AddChild(banner);
        banner.label.SetTextAutoSize(new LocString("combat_messages", "YGODUELIST-SEALED_PACK.banner").GetRawText());
        banner.AnimateIn();

        // Vertical ExpandFill steals all space below the banner; bottom HBox then gets height 0 and Back/Confirm stay 0×0.
        var row = new HBoxContainer
        {
            Name = "SealedPackRow",
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        row.AddThemeConstantOverride("separation", 56);
        vbox.AddChild(row);

        for (int i = 0; i < _masks.Count; i++)
        {
            var w = new NYgoSealedPackWidget();
            int idx = i;
            w.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            w.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            w.Build(i, _masks[i]);
            w.Pressed += () => OnPackPressed(idx);
            row.AddChild(w);
            _widgets.Add(w);
        }

        _packRow = row;
        GetTree().CreateTimer(0.05, processAlways: false, ignoreTimeScale: true).Timeout += () => DebugDumpFullLayout("timer+0.05s");
        GetTree().CreateTimer(0.35, processAlways: false, ignoreTimeScale: true).Timeout += () => DebugDumpFullLayout("timer+0.35s");

        var bottom = new HBoxContainer
        {
            Name = "SealedPackBottomBar",
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(0f, 96f)
        };
        bottom.AddThemeConstantOverride("separation", 24);
        vbox.AddChild(bottom);
        _bottomBar = bottom;

        _backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button")).Instantiate<NBackButton>(PackedScene.GenEditState.Disabled);
        _backButton.Name = "SealedPackBack";
        _backButton.Visible = true;
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCancel()));
        bottom.AddChild(WrapBottomChromeSlot("SealedPackBackSlot", _backButton));

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bottom.AddChild(spacer);

        PackedScene confirmScene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/card_selection/simple_card_select_screen"));
        var confirmSrc = confirmScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        _confirmButton = confirmSrc.GetNodeOrNull<NConfirmButton>("%Confirm")
            ?? throw new InvalidOperationException(
                "NYgoSealedPackSelectionScreen: %Confirm missing from simple_card_select_screen scene.");
        _confirmButton = (NConfirmButton)_confirmButton.Duplicate();
        _confirmButton.Name = "SealedPackConfirm";
        _confirmButton.Disable();
        _confirmButton.Visible = true;
        _confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnConfirm()));
        bottom.AddChild(WrapBottomChromeSlot("SealedPackConfirmSlot", _confirmButton));

        confirmSrc.QueueFree();
        Callable.From(() => DebugDumpFullLayout("_Ready deferred")).CallDeferred();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!_completion.Task.IsCompleted)
            _completion.TrySetCanceled();
    }

    public async Task<int> WaitPackResultAsync()
    {
        try
        {
            int r = await _completion.Task;
            NOverlayStack.Instance?.Remove(this);
            return r;
        }
        catch (OperationCanceledException)
        {
            if (GodotObject.IsInstanceValid(this))
                NOverlayStack.Instance?.Remove(this);
            throw;
        }
    }

    private void OnPackPressed(int index)
    {
        _selectedIndex = index;
        for (int i = 0; i < _widgets.Count; i++)
            _widgets[i].SetPackSelectionFocused(i == index);

        _confirmButton?.Enable();
    }

    private void OnConfirm()
    {
        if (_selectedIndex < 0)
            return;
        _completion.TrySetResult(_selectedIndex);
    }

    private void OnCancel() => _completion.TrySetCanceled();

    /// <summary>
    /// <see cref="NBackButton"/> / <see cref="NConfirmButton"/> report 0×0 minimum under <see cref="HBoxContainer"/> with
    /// <c>LayoutMode = Container</c>. Fixed slots + anchor fill match <see cref="SimpleCardSelectScreenCancelBackButtonPatch"/> (anchors, not container).
    /// </summary>
    private static Control WrapBottomChromeSlot(string slotName, Control chrome)
    {
        var slot = new Control
        {
            Name = slotName,
            CustomMinimumSize = new Vector2(220f, 88f),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        slot.AddChild(chrome);
        chrome.LayoutMode = 1;
        chrome.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        chrome.MouseFilter = Control.MouseFilterEnum.Stop;
        return slot;
    }

    private void DebugDumpFullLayout(string tag)
    {
        Vector2 vps = GetViewport().GetVisibleRect().Size;
        Rect2 screenG = GetGlobalRect();
        GD.PrintErr(
            $"[YgoSealedPack] layout_dump [{tag}] viewport={vps} screen name={Name} visible={Visible} modulate={Modulate} " +
            $"global={screenG.Position} size={screenG.Size} layout_mode={LayoutMode}");

        void DumpCtrl(string label, Control? c)
        {
            if (c == null || !GodotObject.IsInstanceValid(c))
            {
                GD.PrintErr($"[YgoSealedPack] layout_dump [{tag}] {label}: null");
                return;
            }

            Rect2 g = c.GetGlobalRect();
            GD.PrintErr(
                $"[YgoSealedPack] layout_dump [{tag}] {label}: type={c.GetType().Name} name={c.Name} visible={c.Visible} " +
                $"modulate={c.Modulate} layout={c.LayoutMode} global={g.Position} size={g.Size} min={c.CustomMinimumSize}");
        }

        DumpCtrl("rootMargin", _rootMargin);
        DumpCtrl("mainVBox", _mainVBox);
        DumpCtrl("packRow", _packRow);
        DumpCtrl("bottomBar", _bottomBar);
        DumpCtrl("backButton", _backButton);
        DumpCtrl("confirmButton", _confirmButton);

        if (_packRow != null && GodotObject.IsInstanceValid(_packRow))
        {
            int sep = _packRow.GetThemeConstant("separation", "BoxContainer");
            GD.PrintErr($"[YgoSealedPack] layout_dump [{tag}] packRow separation={sep} children={_packRow.GetChildCount()}");
            for (int i = 0; i < _packRow.GetChildCount(); i++)
            {
                if (_packRow.GetChild(i) is Control wc)
                {
                    Rect2 wg = wc.GetGlobalRect();
                    GD.PrintErr(
                        $"[YgoSealedPack] layout_dump [{tag}] pack[{i}] {wc.GetType().Name} global={wg.Position} size={wg.Size} visible={wc.Visible}");
                }
            }
        }

        if (_bottomBar != null && GodotObject.IsInstanceValid(_bottomBar))
        {
            GD.PrintErr($"[YgoSealedPack] layout_dump [{tag}] bottomBar children={_bottomBar.GetChildCount()}");
            for (int j = 0; j < _bottomBar.GetChildCount(); j++)
            {
                if (_bottomBar.GetChild(j) is Control bc)
                {
                    Rect2 bg = bc.GetGlobalRect();
                    GD.PrintErr(
                        $"[YgoSealedPack] layout_dump [{tag}] bottom[{j}] {bc.GetType().Name} name={bc.Name} global={bg.Position} size={bg.Size} visible={bc.Visible}");
                }
            }
        }
    }

    private static NCommonBanner DuplicateBanner()
    {
        var root = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath("screens/card_selection/choose_a_card_selection_screen"))
            .Instantiate<Control>(PackedScene.GenEditState.Disabled);
        var b = root.GetNode<NCommonBanner>("Banner");
        var dup = (NCommonBanner)b.Duplicate();
        root.QueueFree();
        return dup;
    }

    public void AfterOverlayOpened()
    {
        Modulate = Colors.Transparent;
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1f, 0.35);
    }

    public void AfterOverlayClosed()
    {
        _fadeTween?.Kill();
        QueueFree();
    }

    public void AfterOverlayShown() => Visible = true;

    public void AfterOverlayHidden() => Visible = false;
}
