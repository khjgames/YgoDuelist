namespace YgoDuelist.YgoDuelistCode.Services;

public enum TrunkSideDeckEditorPage
{
    Trunk = 0,
    Side = 1,
    Split = 2
}

/// <summary>
/// Persists which trunk/side editor page is active between relic opens; coordinates page switches from nav buttons.
/// </summary>
public static class TrunkSideDeckEditorSession
{
    public static TrunkSideDeckEditorPage ActivePage { get; set; } = TrunkSideDeckEditorPage.Trunk;

    private static TrunkSideDeckEditorPage? _pendingNavigateTo;

    public static void RequestNavigateTo(TrunkSideDeckEditorPage page) => _pendingNavigateTo = page;

    public static bool TryConsumeNavigateRequest(out TrunkSideDeckEditorPage page)
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
