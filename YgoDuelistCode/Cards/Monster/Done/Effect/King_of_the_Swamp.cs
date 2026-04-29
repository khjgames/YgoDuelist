using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class King_of_the_Swamp : EffectMonsterCard, IFusionMaterialSubstitute
{
    private static readonly LocString SearchPrompt =
        new LocString("combat_messages", "KING_OF_THE_SWAMP_HAND_EFFECT_SELECT");

    private static readonly YgoSearchPile[] SearchPiles =
    [
        YgoSearchPile.Draw,
        YgoSearchPile.Discard
    ];

    public King_of_the_Swamp()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Fusion;

    public override Type[] RelatedCards => new[]
    {
        typeof(King_of_the_Swamp),
        typeof(Polymerization),
    };

    protected override bool SupportsHandEffectForm => true;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;

    protected override PileType GetResultPileType()
    {
        if (IsHandEffectFormActive)
            return GraveyardPile.CustomType;

        return base.GetResultPileType();
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            if (!IsHandEffectFormActive || Owner == null)
                return true;

            return YgoPileSearchSelection.BuildCandidates<Polymerization>(Owner, SearchPiles).Count >= 1;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            if (Owner?.Creature != null)
                await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            await ResolveHandEffectSearchAsync();
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    private async Task ResolveHandEffectSearchAsync()
    {
        Player? player = Owner;
        if (player == null)
            return;

        await YgoPileSearchSelection.TrySearchToHandAsync<Polymerization>(
            player,
            SearchPrompt,
            SearchPiles,
            minSelect: 1,
            maxSelect: 1,
            cancelable: true,
            requireManualConfirmation: true);
    }
}