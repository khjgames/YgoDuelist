using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Battle death: you can pay {Mgc} HP to Special Summon this card from your Graveyard.</summary>
public sealed class Revival_Jam : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-REVIVAL_JAM.activate_effect");

    public Revival_Jam()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 5,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Ocean;
    public override Type[] RelatedCards => new[] { typeof(Revival_Jam) };

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(this))
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(RunBattleDeathAsync(player));
    }

    private async Task RunBattleDeathAsync(Player player)
    {
        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;

        int hpCost = (int)DynamicVars["Mgc"].BaseValue;
        if (hpCost > 0 && player.Creature != null)
        {
            if (player.Creature.CurrentHp <= hpCost)
                return;
            await CreatureCmd.SetCurrentHp(player.Creature, player.Creature.CurrentHp - hpCost);
        }

        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;

        FaceDown = false;
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, ctx);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
