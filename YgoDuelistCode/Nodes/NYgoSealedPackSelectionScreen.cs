using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Pick one of three sealed packs, then confirm — one <see cref="NOverlayStack"/> screen (shared dimmer on). Bottom chrome uses the
/// same <c>%Close</c> / <c>%Confirm</c> as <see cref="NDeckCardSelectScreen"/> (trunk/side deck), as <b>direct children</b> of
/// this full-rect screen — <see cref="NBackButton"/> / <see cref="NConfirmButton"/> tween to window/game coordinates and break inside an <see cref="HBoxContainer"/> slot.
/// </summary>
public partial class NYgoSealedPackSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private IReadOnlyList<YgoCardPackTags> _masks = null!;

    private readonly List<NYgoSealedPackWidget> _widgets = new();
    private readonly TaskCompletionSource<int> _completion = new();

    private int _selectedIndex = -1;
    private NBackButton? _backButton;
    private NConfirmButton? _confirmButton;
    private bool _bannerFadeStarted;
    private HBoxContainer? _packRow;
    private Control? _bottomPad;
    private VBoxContainer? _mainVBox;
    private MarginContainer? _rootMargin;
    private CenterContainer? _bannerCenter;
    private NCommonBanner? _banner;
    private Tween? _bannerModulateTween;

    /// <summary>Space above the banner row (pushes title banner + packs + bar down).</summary>
    private const float BannerTopInset = 170f;

    /// <summary>Extra space below banner before pack row (loot-style layout uses more vertical gap).</summary>
    private const float PackRowTopSpacing = 55f;

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _widgets.Count > 0 ? _widgets[0] : this;

    /// <summary>
    /// Pushes on the next idle frame so a just-closed card grid (<c>_ExitTree</c>) does not leave
    /// <see cref="NOverlayStack"/> busy (<c>add_child</c> / <c>move_child</c> errors).
    /// </summary>
    public static Task<NYgoSealedPackSelectionScreen> PushAsync(IReadOnlyList<YgoCardPackTags> masks)
    {
        var screen = new NYgoSealedPackSelectionScreen();
        screen.Name = nameof(NYgoSealedPackSelectionScreen);
        screen._masks = masks;

        var ready = new TaskCompletionSource<NYgoSealedPackSelectionScreen>();
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(screen))
            {
                ready.TrySetCanceled();
                return;
            }

            NOverlayStack? stack = NOverlayStack.Instance;
            if (stack == null)
            {
                ready.TrySetCanceled();
                return;
            }

            stack.Push(screen);
            ready.TrySetResult(screen);
        }).CallDeferred();

        return ready.Task;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Stop;
        Modulate = Colors.White;

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

        var aboveBannerSpacer = new Control
        {
            Name = "SealedPackAboveBannerSpacer",
            CustomMinimumSize = new Vector2(0f, BannerTopInset),
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        vbox.AddChild(aboveBannerSpacer);

        var bannerCenter = new CenterContainer
        {
            Name = "SealedPackBannerCenter",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        vbox.AddChild(bannerCenter);
        _bannerCenter = bannerCenter;

        NCommonBanner banner = DuplicateBanner();
        banner.Name = "SealedPackBanner";
        banner.LayoutMode = 2;
        banner.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        banner.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        banner.Visible = true;
        bannerCenter.AddChild(banner);
        _banner = banner;
        banner.label.SetTextAutoSize(new LocString("combat_messages", "YGODUELIST-SEALED_PACK.banner").GetRawText());
        banner.TopLevel = false;
        DisconnectCommonBannerViewportResizeListener(banner);
        banner.Position = Vector2.Zero;
        LogBannerDebug("after_AddChild+disconnect");

        var belowBannerSpacer = new Control
        {
            Name = "SealedPackBelowBannerSpacer",
            CustomMinimumSize = new Vector2(0f, PackRowTopSpacing),
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        vbox.AddChild(belowBannerSpacer);

        // Pack row stays shrink-height; bottom pad reserves space so content does not sit under floating deck chrome.
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

        var bottomPad = new Control
        {
            Name = "SealedPackBottomPad",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(0f, 96f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        vbox.AddChild(bottomPad);
        _bottomPad = bottomPad;

        AttachDeckFloatingChrome();

        GetTree().CreateTimer(0.05, processAlways: false, ignoreTimeScale: true).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(this))
                DebugDumpFullLayout("timer+0.05s");
        };
        GetTree().CreateTimer(0.35, processAlways: false, ignoreTimeScale: true).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(this))
                DebugDumpFullLayout("timer+0.35s");
        };

        Callable.From(() => DebugDumpFullLayout("_Ready deferred")).CallDeferred();

        Callable.From(DeferredBannerShowPass1).CallDeferred();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
            LogBannerDebug("NotificationVisibilityChanged");
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
            // Defer Remove: calling from the confirm button stack runs inside NOverlayStack.Remove's signal path;
            // vanilla ScreenStateTracker then Connect(Completed, ...) twice → "Signal 'Completed' is already connected".
            Callable.From(RemoveSelfFromOverlayStackIfValid).CallDeferred();
            return r;
        }
        catch (OperationCanceledException)
        {
            Callable.From(RemoveSelfFromOverlayStackIfValid).CallDeferred();
            throw;
        }
    }

    private void RemoveSelfFromOverlayStackIfValid()
    {
        if (!GodotObject.IsInstanceValid(this))
            return;
        NOverlayStack.Instance?.Remove(this);
    }

    /// <summary>
    /// Same <c>%Close</c> / <c>%Confirm</c> as <see cref="NDeckCardSelectScreen"/>: duplicated as siblings of the root margin on this
    /// full-rect overlay. Those controls assume a full-screen parent (they tween to <see cref="Viewport"/> / <see cref="NGame"/> coordinates).
    /// </summary>
    private void AttachDeckFloatingChrome()
    {
        PackedScene deckScene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/card_selection/deck_card_select_screen"));
        var deckSrc = deckScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        NBackButton? closeSrc = deckSrc.GetNodeOrNull<NBackButton>("%Close");
        NConfirmButton? confirmSrc = deckSrc.GetNodeOrNull<NConfirmButton>("%Confirm");
        if (closeSrc == null || confirmSrc == null)
        {
            deckSrc.QueueFree();
            throw new InvalidOperationException(
                "NYgoSealedPackSelectionScreen: deck_card_select_screen must expose %Close and %Confirm (same as NDeckCardSelectScreen).");
        }

        _backButton = (NBackButton)closeSrc.Duplicate();
        _confirmButton = (NConfirmButton)confirmSrc.Duplicate();
        deckSrc.QueueFree();

        _backButton.Name = "SealedPackClose";
        _backButton.Visible = true;
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCancel()));
        AddChild(_backButton);
        // NBackButton._Ready ends with OnDisable(); Enable() must run after AddChild or the button stays off-screen.
        _backButton.Enable();

        _confirmButton.Name = "SealedPackConfirm";
        _confirmButton.Visible = true;
        _confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnConfirm()));
        AddChild(_confirmButton);

        LogChromeDebug("AttachDeckFloatingChrome_immediate");
        Callable.From(() => LogChromeDebug("AttachDeckFloatingChrome_deferred")).CallDeferred();
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

    private void LogChromeDebug(string tag)
    {
        if (!CanLogLayoutDebug())
            return;

        Window w = GetWindow();
        Vector2 content = w.ContentScaleSize;
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2? gameSize = GodotObject.IsInstanceValid(NGame.Instance) ? NGame.Instance.Size : null;
        GD.PrintErr(
            $"[YgoSealedPackChrome] {tag} contentScaleSize={content} viewportVisible={viewport} nGameSize={gameSize}");

        void Dump(string label, Control? c)
        {
            if (c == null || !GodotObject.IsInstanceValid(c))
            {
                GD.PrintErr($"[YgoSealedPackChrome] {tag} {label}: null");
                return;
            }

            Control? p = c.GetParent() as Control;
            Rect2 g = c.GetGlobalRect();
            GD.PrintErr(
                $"[YgoSealedPackChrome] {tag} {label} parent={(p == null ? "null" : $"{p.GetType().Name}:{p.Name}")} " +
                $"topLevel={c.TopLevel} layoutMode={c.LayoutMode} pos={c.Position} size={c.Size} globalRect={g.Position} {g.Size} " +
                $"anchors L={c.AnchorLeft} T={c.AnchorTop} R={c.AnchorRight} B={c.AnchorBottom}");
        }

        Dump("back", _backButton);
        Dump("confirm", _confirmButton);
    }

    private void DeferredBannerShowPass1()
    {
        LogBannerDebug("DeferredBannerShowPass1");
        Callable.From(DeferredBannerShowPass2).CallDeferred();
    }

    private void DeferredBannerShowPass2()
    {
        LogBannerDebug("DeferredBannerShowPass2");
        if (_banner != null && GodotObject.IsInstanceValid(_banner))
        {
            _banner.TopLevel = false;
            DisconnectCommonBannerViewportResizeListener(_banner);
        }

        // Re-run CenterContainer layout without touching child Position (zero would pin banner to local origin = left).
        _bannerCenter?.QueueSort();
        StartBannerModulateFadeIn();
    }

    /// <summary>
    /// <see cref="NCommonBanner"/> mixes local <see cref="Control.Position"/> math with a <c>global_position</c> tween in
    /// <c>AnimateIn</c>; under a <see cref="CenterContainer"/> that snaps the banner to the viewport top. We keep container layout
    /// and only fade <see cref="CanvasItem.Modulate"/>.
    /// </summary>
    private void StartBannerModulateFadeIn()
    {
        if (_bannerFadeStarted)
            return;
        if (_banner == null || !GodotObject.IsInstanceValid(_banner))
        {
            LogBannerDebug("StartBannerModulateFadeIn_skip_no_banner");
            return;
        }

        _bannerFadeStarted = true;
        _bannerModulateTween?.Kill();
        _banner.Modulate = new Color(1f, 1f, 1f, 0f);
        LogBannerDebug("StartBannerModulateFadeIn_begin");
        _bannerModulateTween = CreateTween();
        _bannerModulateTween.TweenProperty(_banner, "modulate:a", 1f, 0.4)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _bannerModulateTween.Finished += () => LogBannerDebug("StartBannerModulateFadeIn_tweenFinished");
    }

    private static bool CanLogLayoutDebug(Control screen) =>
        GodotObject.IsInstanceValid(screen) && screen.IsInsideTree() && screen.GetViewport() != null;

    private bool CanLogLayoutDebug() => CanLogLayoutDebug(this);

    private void LogBannerDebug(string tag)
    {
        if (!CanLogLayoutDebug())
            return;

        Rect2 vr = GetViewport()!.GetVisibleRect();
        Rect2 screenG = GetGlobalRect();
        GD.PrintErr(
            $"[YgoSealedPackBanner] {tag} screen visible={Visible} modulate={Modulate} globalRect={screenG.Position} {screenG.Size} " +
            $"viewportRect={vr.Position} {vr.Size}");

        if (_banner == null || !GodotObject.IsInstanceValid(_banner))
        {
            GD.PrintErr($"[YgoSealedPackBanner] {tag} banner=null");
            return;
        }

        Rect2 bg = _banner.GetGlobalRect();
        Control? parent = _banner.GetParent() as Control;
        Rect2? pg = parent != null ? parent.GetGlobalRect() : null;
        GD.PrintErr(
            $"[YgoSealedPackBanner] {tag} banner visible={_banner.Visible} modulate={_banner.Modulate} topLevel={_banner.TopLevel} " +
            $"layoutMode={_banner.LayoutMode} pos={_banner.Position} size={_banner.Size} globalRect={bg.Position} {bg.Size} " +
            $"globalPos={_banner.GlobalPosition} " +
            $"parent={(parent == null ? "null" : parent.GetType().Name + ":" + parent.Name)} parentGlobal={pg?.Position} {pg?.Size}");

        var labRef = _banner.label;
        if (labRef != null && GodotObject.IsInstanceValid(labRef) && labRef is Control labCtrl)
        {
            Rect2 lg = labCtrl.GetGlobalRect();
            int textLen = labCtrl is Label plain ? plain.Text.Length : -1;
            GD.PrintErr(
                $"[YgoSealedPackBanner] {tag} label type={labCtrl.GetType().Name} visible={labCtrl.Visible} modulate={labCtrl.Modulate} " +
                $"textLen={textLen} globalRect={lg.Position} {lg.Size} globalPos={labCtrl.GlobalPosition}");
        }
        else
            GD.PrintErr($"[YgoSealedPackBanner] {tag} label missing or not Control (labelRef={(labRef == null ? "null" : labRef.GetType().Name)})");

        if (_bannerCenter != null && GodotObject.IsInstanceValid(_bannerCenter))
        {
            Rect2 bc = _bannerCenter.GetGlobalRect();
            GD.PrintErr($"[YgoSealedPackBanner] {tag} bannerCenter type={_bannerCenter.GetType().Name} globalRect={bc.Position} {bc.Size}");
        }

        if (_packRow != null && GodotObject.IsInstanceValid(_packRow))
        {
            Rect2 pr = _packRow.GetGlobalRect();
            float deltaY = pr.GetCenter().Y - bg.GetCenter().Y;
            GD.PrintErr(
                $"[YgoSealedPackBanner] {tag} packRow globalRect={pr.Position} {pr.Size} centerY={pr.GetCenter().Y} " +
                $"deltaPackCenterY_minus_bannerCenterY={deltaY}");
        }
        else
            GD.PrintErr($"[YgoSealedPackBanner] {tag} packRow=null");
    }

    private static void DisconnectCommonBannerViewportResizeListener(NCommonBanner banner)
    {
        Window root = banner.GetTree().Root;
        StringName sig = Viewport.SignalName.SizeChanged;
        var list = root.GetSignalConnectionList(sig);
        int removed = 0;
        foreach (Variant item in list)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;
            var d = item.AsGodotDictionary();
            if (!d.ContainsKey("callable"))
                continue;
            Callable cb = d["callable"].AsCallable();
            if (cb.Target == banner)
            {
                root.Disconnect(sig, cb);
                removed++;
            }
        }

        GD.PrintErr($"[YgoSealedPackBanner] DisconnectViewportResize removed={removed} for banner={banner.Name}");
    }

    private void DebugDumpFullLayout(string tag)
    {
        if (!CanLogLayoutDebug())
            return;

        LogBannerDebug($"layout_dump_hook:{tag}");
        Vector2 vps = GetViewport()!.GetVisibleRect().Size;
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
        DumpCtrl("bottomPad", _bottomPad);
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

        LogChromeDebug($"layout_dump_hook:{tag}");
    }

    /// <summary>Same <see cref="NCommonBanner"/> node as card loot / <see cref="NCardRewardSelectionScreen"/> (<c>UI/Banner</c>).</summary>
    private static NCommonBanner DuplicateBanner()
    {
        var root = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath("screens/card_selection/card_reward_selection_screen"))
            .Instantiate<Control>(PackedScene.GenEditState.Disabled);
        var b = root.GetNode<NCommonBanner>("UI/Banner");
        var dup = (NCommonBanner)b.Duplicate();
        root.QueueFree();
        return dup;
    }

    public void AfterOverlayOpened()
    {
        Modulate = Colors.White;
        LogBannerDebug("AfterOverlayOpened");
    }

    public void AfterOverlayClosed()
    {
        _bannerModulateTween?.Kill();
        QueueFree();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
        LogBannerDebug("AfterOverlayShown");
        if (_banner != null && GodotObject.IsInstanceValid(_banner))
        {
            _banner.TopLevel = false;
            DisconnectCommonBannerViewportResizeListener(_banner);
            StartBannerModulateFadeIn();
        }

        _bannerCenter?.QueueSort();
        RefreshPackSelectFloatingChrome();

        Callable.From(() =>
        {
            _bannerCenter?.QueueSort();
            RefreshPackSelectFloatingChrome();
            LogBannerDebug("AfterOverlayShown+1frame");
        }).CallDeferred();
    }

    public void AfterOverlayHidden()
    {
        LogBannerDebug("AfterOverlayHidden");
        Visible = false;
        if (_backButton != null && GodotObject.IsInstanceValid(_backButton))
            _backButton.Disable();
        if (_confirmButton != null && GodotObject.IsInstanceValid(_confirmButton))
            _confirmButton.Disable();
    }

    /// <summary>Keep deck chrome aligned with overlay visibility (same idea as <c>SimpleCardSelectScreenCancelBackButtonPatch</c>).</summary>
    private void RefreshPackSelectFloatingChrome()
    {
        if (_backButton != null && GodotObject.IsInstanceValid(_backButton))
            _backButton.Enable();
        if (_confirmButton != null && GodotObject.IsInstanceValid(_confirmButton))
        {
            if (_selectedIndex >= 0)
                _confirmButton.Enable();
            else
                _confirmButton.Disable();
        }
    }
}
