using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Castle_of_Dark_Illusions : EffectMonsterCard, IMonsterFlipEffect
{
    public Castle_of_Dark_Illusions()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 19,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie | YgoCardPackTags.Fiend;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.ZombieBoost;

    public override Type[] RelatedCards => GetRelatedCards();

    public override Type[] BundledCards => new[] { typeof(Pumpking_the_King_of_Ghosts) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<NecroticRitualPower>();
            yield return HoverTipFactory.FromPower<NecroticEvolutionPower>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Castle_of_Dark_Illusions || Owner?.Creature == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
        if (pet == null)
            return;

        decimal add = DynamicVars["Mgc"].BaseValue;
        if (add <= 0m)
            return;

        if (pet.GetPower<NecroticRitualPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, add, Owner.Creature, this);
        else
            await PowerCmd.Apply<NecroticRitualPower>(pet, add, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 6;
    }
}
