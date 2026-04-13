using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Insect_Queen : EffectMonsterCard
{
    public Insect_Queen()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 24,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Insect_Queen), typeof(Insect_Monster_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Insect_Monster_Token));

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int others = 0;
        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner);
        foreach (BaseMonsterCard? m in field)
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, this))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Insect)
                others++;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (others * mgc, 0);
    }

    protected override async Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        if (Owner == null)
            return;

        foreach (var r in attackCommand.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                continue;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Insect_Monster_Token>(Owner, choiceContext, defensePosition: false);
            break;
        }
    }
}
