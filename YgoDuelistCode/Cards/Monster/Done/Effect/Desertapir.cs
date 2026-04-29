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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Desertapir : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt = new("cards", "YGODUELIST-DESERTAPIR.flip_target");

    public Desertapir()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Desertapir) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Desertapir flipper || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<AbstractMonsterCard> BuildFlipTargets()
        {
            var candidates = new List<AbstractMonsterCard>();
            foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
            {
                foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(p))
                {
                    if (m.FaceDown || m is not AbstractMonsterCard am)
                        continue;
                    if (m is Desertapir)
                        continue;
                    if (ReferenceEquals(m, flipper))
                        continue;
                    candidates.Add(am);
                }
            }

            return YgoMpCombatOrder.CardsSnapshotOrderedForMp(candidates).OfType<AbstractMonsterCard>().ToList();
        }

        List<AbstractMonsterCard> candidates = BuildFlipTargets();
        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(FlipTargetPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        AbstractMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            prefs,
            BuildFlipTargets);
        if (target?.Owner == null)
            return;
        if (target is not NormalMonsterCard targetNormal)
            return;

        target.FaceDown = true;
        await target.ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(choiceContext, target.Owner, attackPosition: false);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(targetNormal, target.Owner);
        if (pet != null && target.Owner.Creature != null)
            await DuelMonsterStancePowerSync.SyncForPetAsync(pet, target, target.Owner.Creature, targetNormal);
    }
}
