using System;
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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Lady_Panther : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString ReturnPrompt = new("cards", "YGODUELIST-LADY_PANTHER.return_monster_to_draw");

    public Lady_Panther()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Lady_Panther) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-LADY_PANTHER.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && BuildGraveyardMonsterTargets(Owner).Count > 0
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        if (source is not Lady_Panther panther || panther.Owner?.Creature == null)
            return;

        Player player = panther.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(panther, player);
        if (pet == null)
            return;

        List<BaseMonsterCard> candidates = BuildGraveyardMonsterTargets(player);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(ReturnPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardMonsterTargets(player));
        if (chosen == null)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return;

        await CreatureCmd.Kill(pet, force: true);
        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseMonsterCard> BuildGraveyardMonsterTargets(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .ToList();
}
