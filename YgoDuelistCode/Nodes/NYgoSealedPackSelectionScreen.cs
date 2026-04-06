using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Nodes;

/// <summary>
/// Pick one of three sealed packs, then confirm — same chrome as cancelable grid picks (<see cref="NBackButton"/> + <see cref="NConfirmButton"/>).
/// </summary>
public partial class NYgoSealedPackSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private IReadOnlyList<YgoCardPackTags> _masks = null!;

    private readonly List<NYgoSealedPackWidget> _widgets = new();
    private readonly TaskCompletionSource<int> _completion = new();

    private int _selectedIndex = -1;
    private NConfirmButton? _confirmButton;
    private Button? _fallbackConfirm;
    private NBackButton? _backButton;
    private HBoxContainer? _packRow;

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _widgets.Count > 0 ? _widgets[0] : this;

    public static NYgoSealedPackSelectionScreen Push(IReadOnlyList<YgoCardPackTags> masks)
    {
        var screen = new NYgoSealedPackSelectionScreen();
        screen.Name = nameof(NYgoSealedPackSelectionScreen);
        screen._masks = masks;
        GD.PrintErr($"[YgoSealedPack] Push sealed screen masks={masks.Count}");
        NOverlayStack.Instance!.Push(screen);
        return screen;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Stop;
        GD.PrintErr($"[YgoSealedPack] _Ready enter size={Size} min={CustomMinimumSize} parent={(GetParent()?.Name.ToString() ?? "null")}");

        var root = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(root);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddChild(vbox);

        NCommonBanner banner = DuplicateBanner();
        vbox.AddChild(banner);
        banner.label.SetTextAutoSize(new LocString("combat_messages", "YGODUELIST-SEALED_PACK.banner").GetRawText());
        banner.AnimateIn();

        var row = new HBoxContainer
        {
            Name = "SealedPackRow",
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
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
        GetTree().CreateTimer(0.05, processAlways: false, ignoreTimeScale: true).Timeout += DeferredDebugPrintPackRow;

        var bottom = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(bottom);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bottom.AddChild(spacer);

        PackedScene confirmScene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/card_selection/simple_card_select_screen"));
        var confirmSrc = confirmScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        _confirmButton = confirmSrc.GetNodeOrNull<NConfirmButton>("%Confirm");
        if (_confirmButton != null)
        {
            _confirmButton = (NConfirmButton)_confirmButton.Duplicate();
            _confirmButton.Disable();
            _confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnConfirm()));
            bottom.AddChild(_confirmButton);
        }
        else
        {
            _fallbackConfirm = new Button();
            _fallbackConfirm.Text = new LocString("combat_messages", "YGODUELIST-SEALED_PACK.confirm").GetRawText();
            _fallbackConfirm.Disabled = true;
            _fallbackConfirm.Pressed += OnConfirm;
            bottom.AddChild(_fallbackConfirm);
        }

        confirmSrc.QueueFree();

        _backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button")).Instantiate<NBackButton>(PackedScene.GenEditState.Disabled);
        _backButton.LayoutMode = 1;
        _backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCancel()));
        AddChild(_backButton);

        GD.PrintErr($"[YgoSealedPack] _Ready exit size={Size} widgets={_widgets.Count}");
    }

    private void DeferredDebugPrintPackRow()
    {
        if (_packRow != null)
            DebugPrintPackRow(_packRow);
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
            _widgets[i].Modulate = i == index ? Colors.White : new Color(0.55f, 0.55f, 0.55f, 1f);

        _confirmButton?.Enable();
        if (_fallbackConfirm != null)
            _fallbackConfirm.Disabled = false;
    }

    private void OnConfirm()
    {
        if (_selectedIndex < 0)
            return;
        _completion.TrySetResult(_selectedIndex);
    }

    private void OnCancel() => _completion.TrySetCanceled();

    private static void DebugPrintPackRow(HBoxContainer row)
    {
        if (!GodotObject.IsInstanceValid(row))
            return;
        int sep = row.GetThemeConstant("separation", "BoxContainer");
        Rect2 rg = row.GetGlobalRect();
        GD.PrintErr(
            $"[YgoSealedPack] row name={row.Name} global_rect={rg.Position} size={rg.Size} " +
            $"child_count={row.GetChildCount()} separation={sep}");
        for (int i = 0; i < row.GetChildCount(); i++)
        {
            if (row.GetChild(i) is not Control c)
                continue;
            Rect2 g = c.GetGlobalRect();
            GD.PrintErr($"[YgoSealedPack] row child[{i}] type={c.GetType().Name} name={c.Name} global_pos={g.Position} size={g.Size}");
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
        Modulate = Colors.White;
    }

    public void AfterOverlayClosed()
    {
        QueueFree();
    }

    public void AfterOverlayShown() => Visible = true;

    public void AfterOverlayHidden() => Visible = false;
}
