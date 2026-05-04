using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Bite_Shoes : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");
    private static readonly LocString FlipPrompt = new("cards", "YGODUELIST-BITE_SHOES.flip_change_position");

    public Bite_Shoes()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 5,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Bite_Shoes) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-BITE_SHOES.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Bite_Shoes || Owner?.Creature?.CombatState == null)
            return;

        List<NormalMonsterCard> BuildTargets()
        {
            var list = new List<NormalMonsterCard>();
            foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(Owner.Creature.CombatState.Players))
            {
                foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(p))
                {
                    if (m.FaceDown || m is not NormalMonsterCard nm)
                        continue;
                    list.Add(nm);
                }
            }

            return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list).OfType<NormalMonsterCard>().ToList();
        }

        List<NormalMonsterCard> candidates = BuildTargets();
        if (candidates.Count == 0)
            return;

        NormalMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            BuildTargets);
        if (target?.Owner == null)
            return;

        await target.ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(
            choiceContext,
            target.Owner,
            attackPosition: !target.IsAttackBattlePosition);
    }
}
