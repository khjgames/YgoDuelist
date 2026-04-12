using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoTokenSummon
{
    /// <summary>Creates a token instance and special-summons it. Returns false if there was no room or summon failed.</summary>
    public static async Task<bool> TrySpecialSummonTokenAsync<TToken>(
        Player player,
        PlayerChoiceContext choiceContext,
        bool defensePosition,
        Action<TToken>? configure = null)
        where TToken : BaseMonsterCard, IYgoTokenMonster
    {
        if (player?.Creature?.CombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;

        var token = (TToken)player.Creature.CombatState.CreateCard<TToken>(player);
        configure?.Invoke(token);
        YgoTokenAlternatePortrait.AssignForSummon(token, player);
        if (defensePosition && token.RegisteredCardType == CardType.Attack)
            token.ToggleAttackSkill(allowCanonicalUiPreview: false);

        return await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, token, choiceContext);
    }

    public static int MaxTokensThatFit(Player? player)
    {
        if (player?.Creature?.CombatState == null)
            return 0;
        int live = DuelMonsterSummon.CountLiveDuelMonsters(player);
        return Math.Max(0, DuelMonsterSummon.MaxDuelMonstersPerPlayer - live);
    }
}
