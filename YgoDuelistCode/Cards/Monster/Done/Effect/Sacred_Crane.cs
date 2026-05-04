using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>On Summon: draw 1 card.</summary>
public sealed class Sacred_Crane : EffectMonsterCard
{
    public Sacred_Crane()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 16,
            baseDef: 4,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Sacred_Crane) };

    public override async Task OnAfterSummonPipelineAsync(
        Player player,
        PlayerChoiceContext ctx,
        Creature pet,
        bool canAttackThisTurn)
    {
        _ = pet;
        _ = canAttackThisTurn;
        if (player.PlayerCombatState == null)
            return;
        await CardPileCmd.Draw(EnsureBlockingChoiceContext(ctx), 1, player);
    }
}
