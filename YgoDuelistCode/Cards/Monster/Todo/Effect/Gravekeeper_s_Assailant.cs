using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>While <see cref="Necrovalley"/> is face-up, this card's attacks apply 1 Weak and {Mgc} Vulnerable.</summary>
public sealed class Gravekeeper_s_Assailant : EffectMonsterCard
{
    public Gravekeeper_s_Assailant()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 15,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Gravekeeper_s_Assailant), typeof(Necrovalley) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    internal static async Task TryApplyNecrovalleyAttackDebuffAsync(
        PlayerChoiceContext choiceContext,
        Gravekeeper_s_Assailant source,
        Player player,
        Creature targetEnemy)
    {
        if (!YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Necrovalley>(player))
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        decimal vuln = source.DynamicVars["Mgc"].BaseValue;
        await PowerCmd.Apply<WeakPower>(targetEnemy, 1m, pet, source);
        if (vuln > 0m)
            await PowerCmd.Apply<VulnerablePower>(targetEnemy, vuln, pet, source);
    }
}
