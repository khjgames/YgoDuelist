using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class SpellTrapZoneRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/spell_trap_zone.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/spell_trap_zone.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => GetZoneCountForOwner();

    private CardPile? _subscribedPile;

    public static CardPile? GetSpellTrapZonePile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, SpellTrapZonePile.CustomType);
    }

    private int GetZoneCountForOwner()
    {
        var player = Owner;
        if (player == null)
            return 0;
        return GetSpellTrapZonePile(player)?.Cards.Count ?? 0;
    }

    public override Task BeforeCombatStart()
    {
        SubscribeToZonePile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromZonePile();
        return Task.CompletedTask;
    }

    private void SubscribeToZonePile()
    {
        var player = Owner;
        if (player == null)
            return;
        var pile = GetSpellTrapZonePile(player);
        if (pile == null)
            return;

        if (_subscribedPile != null)
            UnsubscribeFromZonePile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnZoneContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromZonePile()
    {
        if (_subscribedPile == null)
            return;
        _subscribedPile.ContentsChanged -= OnZoneContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnZoneContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    public static bool IsSpellTrapZoneRelic(RelicModel? model) => model is SpellTrapZoneRelic;
}
