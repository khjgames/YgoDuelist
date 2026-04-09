using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Obelisk_the_Tormentor : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Obelisk_the_Tormentor()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Divine,
            baseAtk: 40,
            baseDef: 40,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.DivineBeast,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.God | YgoCardPackTags.WinCon;

    public override Type[] RelatedCards => new[] { typeof(Obelisk_the_Tormentor) };

    protected override int? TributeReleaseCountOverride => 3;

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-OBELISK_THE_TORMENTOR.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && TributeSummonSelection.BuildTributeCandidateCards(Owner).Count(c => !ReferenceEquals(c, this)) >= 2;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;

        var candidates = TributeSummonSelection
            .BuildTributeCandidateCards(player)
            .Where(c => !ReferenceEquals(c, source))
            .ToList();

        if (candidates.Count < 2)
            return false;

        var prefs = new CardSelectorPrefs(TributePrompt, 2, 2)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var list = picked.OfType<BaseMonsterCard>().ToList();
        if (list.Count != 2 || ReferenceEquals(list[0], list[1]))
            return false;

        ObeliskActivatedTributePayload.SetPending(source, list);
        return true;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature?.CombatState == null || pet == null)
            return;

        if (!ObeliskActivatedTributePayload.TryTakePending(source, out var tributes) || tributes == null)
            return;

        foreach (BaseMonsterCard tributeCard in tributes)
        {
            Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(player, tributeCard);
            if (tributePet == null || !tributePet.IsAlive)
                return;

            await CreatureCmd.Kill(tributePet, force: true);

            CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
            if (graveyard != null)
                await CardPileCmd.Add(new[] { tributeCard }, graveyard, CardPilePosition.Top, tributeCard, false);
        }

        var field = DuelMonsterFieldRegistry.GetFieldMonsters(player)?.ToList() ?? new List<BaseMonsterCard>();
        if (source is BaseMonsterCard bm && !field.Contains(bm))
            field.Add(bm);

        int blight = source.CalcDuelMonsterStats(field).Atk;
        CombatState cs = player.Creature.CombatState;

        foreach (Creature enemy in cs.HittableEnemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<BlightPower>(enemy, blight, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        const int d = 12;
        DynamicVars.Damage.UpgradeValueBy(d);
        DynamicVars["Def"].UpgradeValueBy(d);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(d);
        SyncPermanentExecuteIncreaseVar();
    }
}
