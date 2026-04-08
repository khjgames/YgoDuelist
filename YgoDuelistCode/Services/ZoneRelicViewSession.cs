namespace YgoDuelist.YgoDuelistCode.Services;

public enum ZoneRelicViewPage
{
    Graveyard = 0,
    ShadowRealm = 1,
    ExtraDeck = 2
}

/// <summary>
/// Active zone viewer page and pending navigation between Graveyard / Shadow Realm / Extra Deck (same pattern as <see cref="TrunkSideDeckEditorSession"/>).
/// </summary>
public static class ZoneRelicViewSession
{
    public static ZoneRelicViewPage ActivePage { get; set; } = ZoneRelicViewPage.Graveyard;

    private static ZoneRelicViewPage? _pendingNavigateTo;

    public static void RequestNavigateTo(ZoneRelicViewPage page) => _pendingNavigateTo = page;

    public static bool TryConsumeNavigateRequest(out ZoneRelicViewPage page)
    {
        if (!_pendingNavigateTo.HasValue)
        {
            page = default;
            return false;
        }

        page = _pendingNavigateTo.Value;
        _pendingNavigateTo = null;
        return true;
    }

    public static void ClearNavigateRequest() => _pendingNavigateTo = null;
}
