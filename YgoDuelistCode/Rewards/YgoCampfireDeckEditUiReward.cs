using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;

namespace YgoDuelist.YgoDuelistCode.Rewards;

/// <summary>
/// Drives <see cref="MegaCrit.Sts2.Core.Nodes.Rewards.NRewardButton"/> for campfire deck-edit UI (not a real map reward).
/// <see cref="OnSelect"/> returns <c>false</c> so vanilla reward hooks do not run; the button stays usable.
/// </summary>
public sealed class YgoCampfireDeckEditUiReward : Reward
{
    private LocString _description;
    private readonly Func<Task> _action;

    protected override RewardType RewardType => RewardType.None;

    public override int RewardsSetIndex => 0;

    public override LocString Description => _description;

    public override bool IsPopulated => true;

    protected override string? IconPath => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_card.png");

    public YgoCampfireDeckEditUiReward(Player player, LocString description, Func<Task> action)
        : base(player)
    {
        _description = description;
        _action = action;
    }

    public void SetDescription(LocString description) => _description = description;

    public override Task Populate() => Task.CompletedTask;

    protected override async Task<bool> OnSelect()
    {
        await _action();
        return false;
    }

    public override void MarkContentAsSeen()
    {
    }
}
