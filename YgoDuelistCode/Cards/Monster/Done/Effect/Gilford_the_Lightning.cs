using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Tribute Summon with 3 tributes: Command Attack hits all enemies.</summary>
public sealed class Gilford_the_Lightning : EffectMonsterCard
{
    [SavedProperty]
    public bool ThreeTributeLightning { get; set; }

    public Gilford_the_Lightning()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Warrior | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Gilford_the_Lightning) };

    protected override int? TributeReleaseCountOverride => 3;

    public override bool DuelMonsterAttackHitsAllEnemies => ThreeTributeLightning;

    protected override void OnBeforeDuelMonsterSummon(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution? tributePending)
    {
        ThreeTributeLightning = tributePending != null
            && tributePending.Pets.Count + tributePending.MausoleumHpTributes >= 3;
    }
}
