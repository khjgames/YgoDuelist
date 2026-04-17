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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>End Phase: returns to hand. Upgraded: Retain.</summary>
public sealed class The_Wicked_Worm_Beast : EffectMonsterCard, IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    public override bool UseAlternateUpgradedDescription => true;

    public The_Wicked_Worm_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    public override Type[] RelatedCards => new[] { typeof(The_Wicked_Worm_Beast) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Concat(
            IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None
                ? new[] { CardKeyword.Retain }
                : Enumerable.Empty<CardKeyword>());

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(
            IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None
                ? new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) }
                : Enumerable.Empty<IHoverTip>());

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        !FaceDown && pet.IsAlive;

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner, Creature pet)
    {
        if (!IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(pet))
            return;
        YgoDuelMonsterBounceToHand.RegisterForHandReturn(pet);
        await CreatureCmd.Kill(pet, force: true);
    }
}
