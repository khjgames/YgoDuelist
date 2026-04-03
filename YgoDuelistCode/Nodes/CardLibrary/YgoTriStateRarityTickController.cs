using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Cycles rarity tickbox through neutral → include (check) → exclude (✕) without replacing the vanilla scene.
/// </summary>
internal sealed class YgoTriStateRarityTickController
{
    static readonly StringName ShaderV = new("v");

    readonly NCardRarityTickbox _box;
    Tween? _imageTween;
    Tween? _labelTween;
    Vector2 _visualBaseScale = Vector2.One;
    Label? _excludeGlyph;
    bool _overlayAdded;

    internal YgoTriStateRarityTickController(NCardRarityTickbox box) => _box = box;

    internal CardLibraryFilterTriState State { get; private set; }

    internal void EnsureOverlay()
    {
        if (_overlayAdded)
            return;
        var visuals = _box.GetNode<Control>("%TickboxVisuals");
        _visualBaseScale = visuals.Scale;
        var wrap = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 4
        };
        wrap.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        visuals.AddChild(wrap);
        _excludeGlyph = new Label
        {
            Text = "✕",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        MegaLabel rowLabel = _box.GetNode<MegaLabel>("Label");
        Font? baseFont = rowLabel.GetThemeFont("font") ?? ThemeDB.FallbackFont;
        _excludeGlyph.AddThemeFontOverride("font", baseFont);
        _excludeGlyph.AddThemeFontSizeOverride("font_size", 24);
        _excludeGlyph.AddThemeColorOverride("font_color", StsColors.gold);
        _excludeGlyph.AddThemeConstantOverride("outline_size", 4);
        _excludeGlyph.AddThemeColorOverride("font_outline_color", StsColors.rewardLabelGoldOutline);
        wrap.AddChild(_excludeGlyph);
        _overlayAdded = true;
        ApplyVisuals();
    }

    internal void SetState(CardLibraryFilterTriState state)
    {
        State = state;
        if (_overlayAdded)
            ApplyVisuals();
        else
            _box.IsTicked = state == CardLibraryFilterTriState.Include;
    }

    internal void OnRelease()
    {
        State = State switch
        {
            CardLibraryFilterTriState.Neutral => CardLibraryFilterTriState.Include,
            CardLibraryFilterTriState.Include => CardLibraryFilterTriState.Exclude,
            _ => CardLibraryFilterTriState.Neutral
        };
        ApplyVisuals();

        if (State == CardLibraryFilterTriState.Include)
            SfxCmd.Play("event:/sfx/ui/clicks/ui_checkbox_on");
        else
            SfxCmd.Play("event:/sfx/ui/clicks/ui_checkbox_off");

        _box.EmitSignal(NTickbox.SignalName.Toggled, _box);

        var imageContainer = _box.GetNode<Control>("%TickboxVisuals");
        var hsv = (ShaderMaterial)imageContainer.Material;
        _imageTween?.Kill();
        _imageTween = _box.CreateTween().SetParallel();
        _imageTween.TweenProperty(imageContainer, "scale", _visualBaseScale * 1.05f, 0.05);
        float v0 = hsv.GetShaderParameter(ShaderV).AsSingle();
        _imageTween.TweenMethod(
            Callable.From<float>(v => hsv.SetShaderParameter(ShaderV, v)),
            v0,
            1.2f,
            0.05);

        MegaLabel label = _box.GetNode<MegaLabel>("Label");
        _labelTween?.Kill();
        _labelTween = _box.CreateTween().SetParallel();
        _labelTween.TweenProperty(label, "scale", Vector2.One * 1.1f, 0.25).SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Bounce);
        _labelTween.TweenProperty(label, "self_modulate", Colors.White, 0.05);
    }

    void ApplyVisuals()
    {
        bool check = State == CardLibraryFilterTriState.Include;
        _box.IsTicked = check;
        if (_excludeGlyph != null)
            _excludeGlyph.Visible = State == CardLibraryFilterTriState.Exclude;
    }
}
