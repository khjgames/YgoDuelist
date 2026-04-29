using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Ryu_Kishin_Clown : EffectMonsterCard
{
    private static readonly LocString PositionPrompt = new("cards", "YGODUELIST-RYU_KISHIN_CLOWN.change_position");

    public Ryu_Kishin_Clown()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 8,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Ryu_Kishin_Clown) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Creatures.Creature duelMonsterPet)
    {
        _ = duelMonsterPet;
        await TryChangeBattlePositionAsync(player, choiceContext);
    }

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player)
    {
        await base.OnFlipSummonedFromCommandMenuAsync(choiceContext, player);
        await TryChangeBattlePositionAsync(player, choiceContext);
    }

    private async Task TryChangeBattlePositionAsync(Player player, PlayerChoiceContext choiceContext)
    {
        if (player.Creature?.CombatState == null)
            return;

        List<NormalMonsterCard> BuildTargets()
        {
            var list = new List<NormalMonsterCard>();
            foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(player.Creature.CombatState.Players))
            {
                foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(p))
                {
                    if (m.FaceDown || m is not NormalMonsterCard nm)
                        continue;
                    list.Add(nm);
                }
            }

            return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list).OfType<NormalMonsterCard>().ToList();
        }

        List<NormalMonsterCard> candidates = BuildTargets();
        if (candidates.Count == 0)
            return;

        NormalMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PositionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            BuildTargets);
        if (target?.Owner == null)
            return;

        await target.ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(
            choiceContext,
            target.Owner,
            attackPosition: !target.IsAttackBattlePosition);
    }
}
