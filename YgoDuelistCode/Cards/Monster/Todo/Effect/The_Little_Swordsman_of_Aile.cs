using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class The_Little_Swordsman_of_Aile : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString TributeSelectionPrompt =
        new("cards", "YGODUELIST-THE_LITTLE_SWORDSMAN_OF_AILE.tribute_selection");

    private int _tributeAtkBonusThisTurn;

    public The_Little_Swordsman_of_Aile()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 8,
            baseDef: 13,
            baseMgc: 7,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-THE_LITTLE_SWORDSMAN_OF_AILE.activated_effect.description";

    public bool ActivatedEffectConsumesOncePerTurnSlot => false;

    public bool IsActivatedEffectAvailable => IsAnotherMonsterControlled(Owner);

    /// <summary>Used by <see cref="Activate_Effect"/> when the source card has not yet set <see cref="CardModel.Owner"/>.</summary>
    public bool IsAnotherMonsterControlled(Player? playerContext)
    {
        Player? player = playerContext ?? Owner;
        if (player?.PlayerCombatState == null)
            return false;

        return TributeSummonSelection.BuildTributeCandidateCards(player)
            .Any(c => !ReferenceEquals(c, this));
    }

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (!ReferenceEquals(target, this) || _tributeAtkBonusThisTurn <= 0)
            return StatEffectTotal.None;

        return new StatEffectTotal(_tributeAtkBonusThisTurn, 0);
    }

    /// <summary>Clears the tribute ATK bonus at end of the player's turn (<see cref="MonsterCommandRegistry.ClearPerTurnExtrasForPlayer"/>).</summary>
    public void ClearTributeAtkBuffForTurnEnd() => _tributeAtkBonusThisTurn = 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not The_Little_Swordsman_of_Aile || Owner?.PlayerCombatState == null)
            return;

        Player player = Owner;

        List<BaseMonsterCard> candidates = TributeSummonSelection.BuildTributeCandidateCards(player)
            .Where(c => !ReferenceEquals(c, this))
            .ToList();
        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(TributeSelectionPrompt, 1, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = false,
        };

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        BaseMonsterCard? tributeCard = pick.OfType<BaseMonsterCard>().FirstOrDefault();
        if (tributeCard == null)
            return;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(player, tributeCard);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { tributeCard }, graveyard, CardPilePosition.Top, tributeCard, false);

        _tributeAtkBonusThisTurn += (int)DynamicVars["Mgc"].BaseValue;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 10m;
    }
}
