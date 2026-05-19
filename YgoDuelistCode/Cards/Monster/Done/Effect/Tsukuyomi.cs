using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. When Normal Summoned or flipped face-up: change 1 other face-up monster to face-down Defense Position.
/// </summary>
public sealed class Tsukuyomi : SpiritEffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt = new("cards", "YGODUELIST-TSUKUYOMI.flip_target");

    public Tsukuyomi()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 11,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster;

    public override Type[] RelatedCards => new[] { typeof(Tsukuyomi) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            ctx => TrySetOneOtherMonsterFaceDownDefenseAsync(player, ctx));

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await RunOnFlipSummonedFromCommandMenuAsync(
            choiceContext,
            ctx => TrySetOneOtherMonsterFaceDownDefenseAsync(player, ctx));

    public Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Tsukuyomi || Owner == null)
            return Task.CompletedTask;
        return TrySetOneOtherMonsterFaceDownDefenseAsync(Owner, choiceContext);
    }

    private async Task TrySetOneOtherMonsterFaceDownDefenseAsync(Player actingPlayer, PlayerChoiceContext choiceContext)
    {
        CombatState? cs = actingPlayer.Creature?.CombatState;
        if (cs == null)
            return;

        List<AbstractMonsterCard> BuildTargets()
        {
            var candidates = new List<AbstractMonsterCard>();
            foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
            {
                foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(p))
                {
                    if (m.FaceDown || m is not AbstractMonsterCard am || m is Tsukuyomi)
                        continue;
                    if (ReferenceEquals(m, this))
                        continue;
                    candidates.Add(am);
                }
            }

            return YgoMpCombatOrder.CardsSnapshotOrderedForMp(candidates).OfType<AbstractMonsterCard>().ToList();
        }

        List<AbstractMonsterCard> candidates = BuildTargets();
        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(FlipTargetPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        AbstractMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            actingPlayer,
            prefs,
            BuildTargets);
        if (target?.Owner == null || target is not NormalMonsterCard targetNormal)
            return;

        target.FaceDown = true;
        await target.ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(choiceContext, target.Owner, attackPosition: false);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(targetNormal, target.Owner);
        if (pet != null && target.Owner.Creature != null)
            await DuelMonsterStancePowerSync.SyncForPetAsync(pet, target, target.Owner.Creature, targetNormal);
    }
}
