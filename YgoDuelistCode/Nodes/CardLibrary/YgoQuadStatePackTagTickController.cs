using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Pack-tag row: neutral → OR check → AND (O) → ✕ → neutral. Excludes still win over OR/O when both apply.
/// </summary>
internal sealed class YgoQuadStatePackTagTickController
{
    static readonly StringName ShaderV = new("v");

    readonly NCardRarityTickbox _box;
    Tween? _imageTween;
    Tween? _labelTween;
    Vector2 _visualBaseScale = Vector2.One;
    Label? _excludeGlyph;
    Label? _requireGlyph;
    bool _overlayAdded;

    internal YgoQuadStatePackTagTickController(NCardRarityTickbox box) => _box = box;

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

        MegaLabel rowLabel = _box.GetNode<MegaLabel>("Label");
        Font? baseFont = rowLabel.GetThemeFont("font") ?? ThemeDB.FallbackFont;

        _excludeGlyph = new Label
        {
            Text = "✖",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        YgoCardLibraryOverlayGlyphStyle.ApplyExcludeOverlayGlyph(_excludeGlyph, baseFont);

        _requireGlyph = new Label
        {
            Text = "O",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        YgoCardLibraryOverlayGlyphStyle.ApplyBoldOverlayGlyph(_requireGlyph, baseFont);

        wrap.AddChild(_excludeGlyph);
        wrap.AddChild(_requireGlyph);
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
            CardLibraryFilterTriState.Include => CardLibraryFilterTriState.RequireAnd,
            CardLibraryFilterTriState.RequireAnd => CardLibraryFilterTriState.Exclude,
            CardLibraryFilterTriState.Exclude => CardLibraryFilterTriState.Neutral,
            _ => CardLibraryFilterTriState.Neutral
        };
        ApplyVisuals();

        if (State == CardLibraryFilterTriState.Include || State == CardLibraryFilterTriState.RequireAnd)
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
        _box.IsTicked = State == CardLibraryFilterTriState.Include;
        if (_excludeGlyph != null)
            _excludeGlyph.Visible = State == CardLibraryFilterTriState.Exclude;
        if (_requireGlyph != null)
            _requireGlyph.Visible = State == CardLibraryFilterTriState.RequireAnd;
    }
}
