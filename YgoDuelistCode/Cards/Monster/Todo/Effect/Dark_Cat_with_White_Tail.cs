using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Dark_Cat_with_White_Tail : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");
    private static readonly LocString BouncePickPrompt =
        new LocString("cards", "YGODUELIST-DARK_CAT_WITH_WHITE_TAIL.flip.selection");

    public Dark_Cat_with_White_Tail()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-DARK_CAT_WITH_WHITE_TAIL.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Dark_Cat_with_White_Tail || Owner?.Creature?.CombatState == null)
            return;

        var player = Owner;
        var cs = player.Creature.CombatState;

        List<NormalMonsterCard> BuildBounceTargets() => DuelMonsterFieldRegistry
            .OrderedFieldMonstersOfType<NormalMonsterCard>(player)
            .Where(m => !ReferenceEquals(m, this))
            .ToList();

        List<NormalMonsterCard> others = BuildBounceTargets();

        if (others.Count > 0)
        {
            var prefs = new CardSelectorPrefs(BouncePickPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            NormalMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                prefs,
                BuildBounceTargets);
            if (chosen != null)
            {
                Creature? bouncePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(chosen);
                if (bouncePet != null && bouncePet.IsAlive)
                {
                    YgoDuelMonsterBounceToHand.RegisterForHandReturn(bouncePet);
                    await CreatureCmd.Kill(bouncePet, force: true);
                }
            }
        }

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
        {
            await PowerCmd.Apply<WeakPower>(enemy, 1m, player.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, 1m, player.Creature, this);
        }
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
