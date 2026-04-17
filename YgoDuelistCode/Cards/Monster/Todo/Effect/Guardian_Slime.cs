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
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Guardian_Slime : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString AddAncientChantPrompt =
        new("cards", "YGODUELIST-GUARDIAN_SLIME.add_ancient_chant");

    public Guardian_Slime()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God | YgoCardPackTags.Ocean | YgoCardPackTags.Water;

    public override Type[] BundledCards => new[] { typeof(Ancient_Chant) };

    public override Type[] RelatedCards => new[] { typeof(Guardian_Slime), typeof(Ancient_Chant) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GUARDIAN_SLIME.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        MonsterCommandState st = MonsterCommandRegistry.GetOrCreate(pet);
        st.GuardianSlimeDestroyAtEndOfOwnerTurn = true;

        await CreatureCmd.GainBlock(player.Creature, 30m, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade() => base.OnUpgrade();

    public override async Task OnOwnerTurnEndFieldCleanupAsync(PlayerChoiceContext ctx, Player owner, Creature pet)
    {
        if (!MonsterCommandRegistry.TryGet(pet, out MonsterCommandState st) || !st.GuardianSlimeDestroyAtEndOfOwnerTurn)
            return;
        await CreatureCmd.Kill(pet, force: true);
    }

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        TaskHelper.RunSafely(RunAncientChantFromGraveyardAsync(player));
    }

    private static async Task RunAncientChantFromGraveyardAsync(Player player)
    {
        List<Ancient_Chant> candidates = CollectAncientChants(player);
        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(AddAncientChantPrompt, 0, 1) { Cancelable = true };
        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Ancient_Chant? chosen = picked.OfType<Ancient_Chant>().FirstOrDefault();
        if (chosen == null)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        if (!candidates.Contains(chosen))
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<Ancient_Chant> CollectAncientChants(Player player)
    {
        var list = new List<Ancient_Chant>();
        Append(PileType.Draw.GetPile(player), list);
        Append(PileType.Discard.GetPile(player), list);
        return list;
    }

    private static void Append(CardPile? pile, List<Ancient_Chant> list)
    {
        if (pile == null)
            return;
        foreach (CardModel c in pile.Cards)
        {
            if (c is Ancient_Chant ac)
                list.Add(ac);
        }
    }
}
