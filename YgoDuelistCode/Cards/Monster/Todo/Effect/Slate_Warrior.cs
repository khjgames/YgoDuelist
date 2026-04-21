using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// FLIP: gain <c>{Mgc}</c> Slate Warrior stacks (+1 ATK/+1 DEF each). If destroyed by battle: all enemies lose 1 Strength.
/// </summary>
public sealed class Slate_Warrior : EffectMonsterCard, IMonsterFlipEffect
{
    public Slate_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 19,
            baseDef: 4,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Warrior | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[]
    {
        typeof(Slate_Warrior),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<SlateWarriorPower>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Slate_Warrior || Owner?.Creature == null)
            return;

        Creature? pet = Owner.PlayerCombatState?.Pets
            .FirstOrDefault(p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == this);
        if (pet == null || !pet.IsAlive)
            return;

        decimal stacks = DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0m)
            return;

        if (pet.GetPower<SlateWarriorPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, stacks, Owner.Creature, this);
        else
            await PowerCmd.Apply<SlateWarriorPower>(pet, stacks, Owner.Creature, this);
    }

    public override async Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true && ctx.Player.Creature?.CombatState != null)
        {
            foreach (Creature enemy in ctx.Player.Creature.CombatState.HittableEnemies)
            {
                if (!enemy.IsAlive)
                    continue;
                await PowerCmd.Apply<StrengthPower>(enemy, -1m, ctx.Player.Creature, this, silent: true);
            }
        }

        await base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 7m;
    }
}
