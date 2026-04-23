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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Obelisk_the_Tormentor : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    /// <summary>Sum of printed DEF added by activated effect; reapplied after full save load (see <c>CardModelFromSerializableMonsterPermanentStatsPatch</c>).</summary>
    [SavedProperty]
    public int ObeliskActivatedEffectPrintedDefBonus { get; set; }

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
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.DivineBeast,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God;

    public override Type[] RelatedCards => new[] { typeof(Obelisk_the_Tormentor) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public override int ShopPriceModifier => 40;

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

        return await YgoActivatedEffectTributeSelection.TryPrepareExactTributesAsync(
            player,
            source,
            candidates,
            tributeCount: 2,
            TributePrompt);
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

            CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
            if (graveyard != null)
                await CardPileCmd.Add(new[] { tributeCard }, graveyard, CardPilePosition.Top, tributeCard, false);
        }

        var field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        if (source is BaseMonsterCard bm && !field.Contains(bm))
            field.Add(bm);

        if (source is Obelisk_the_Tormentor obelisk)
        {
            int flatBonus = (int)obelisk.DynamicVars["Mgc"].BaseValue;
            if (flatBonus > 0)
            {
                obelisk.ApplyPermanentExecuteAtkDelta(flatBonus);
                ApplyPermanentObeliskActivatedEffectDefDelta(obelisk, flatBonus);
            }
        }

        int blight = source.CalcDuelMonsterStats(field).Atk;
        CombatState cs = player.Creature.CombatState;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await PowerCmd.Apply<BlightPower>(enemy, blight, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static void ApplyPermanentObeliskActivatedEffectDefDelta(Obelisk_the_Tormentor card, int delta)
    {
        if (delta == 0)
            return;
        card.AssertMutable();
        card.ObeliskActivatedEffectPrintedDefBonus += delta;
        if (card.DynamicVars != null)
        {
            card.DynamicVars["Def"].BaseValue += delta;
            if (card.DynamicVars.Block != null)
                card.DynamicVars.Block.BaseValue += delta;
        }

        if (card.DeckVersion is Obelisk_the_Tormentor deck && !ReferenceEquals(deck, card))
        {
            deck.ObeliskActivatedEffectPrintedDefBonus += delta;
            if (deck.DynamicVars != null)
            {
                deck.DynamicVars["Def"].BaseValue += delta;
                if (deck.DynamicVars.Block != null)
                    deck.DynamicVars.Block.BaseValue += delta;
            }
        }
    }

    public override void ApplyPostDeserializePrintedStatBonuses()
    {
        base.ApplyPostDeserializePrintedStatBonuses();
        ApplySavedObeliskActivatedEffectDefBonusToPrintedDefense();
    }

    internal void ApplySavedObeliskActivatedEffectDefBonusToPrintedDefense()
    {
        if (ObeliskActivatedEffectPrintedDefBonus == 0 || DynamicVars == null)
            return;
        CardModel template = ModelDb.GetById<CardModel>(Id).ToMutable();
        for (int i = 0; i < CurrentUpgradeLevel; i++)
        {
            template.UpgradeInternal();
            template.FinalizeUpgradeInternal();
        }

        decimal baselineDef = template.DynamicVars["Def"].BaseValue;
        DynamicVars["Def"].BaseValue = baselineDef + ObeliskActivatedEffectPrintedDefBonus;
        if (DynamicVars.Block != null && template.DynamicVars.Block != null)
            DynamicVars.Block.BaseValue = template.DynamicVars.Block.BaseValue + ObeliskActivatedEffectPrintedDefBonus;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedObeliskActivatedEffectDefBonusToPrintedDefense();
    }

    protected override void OnUpgrade()
    {
        const int d = 8;
        DynamicVars.Damage.UpgradeValueBy(d);
        DynamicVars["Def"].UpgradeValueBy(d);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(d);
        SyncPermanentExecuteIncreaseVar();
    }
}
