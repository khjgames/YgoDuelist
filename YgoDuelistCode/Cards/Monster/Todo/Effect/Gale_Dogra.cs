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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Gale_Dogra : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly CardKeyword DoomedKeyword = (CardKeyword)20048;

    private static readonly LocString PickFusionPrompt =
        new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    public Gale_Dogra()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 6,
            baseDef: 6,
            baseMgc: 25,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Fusion | YgoCardPackTags.Insect | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Gale_Dogra) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Append(DoomedKeyword);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromKeyword(DoomedKeyword);
            yield return HoverTipFactory.FromPower<DoomPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GALE_DOGRA.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && PlayerRunExtraDeck.IsYgoDuelistPlayer(Owner)
        && BuildFusionCardsInExtraDeck(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        List<FusionMonsterCard> fusionTargets = BuildFusionCardsInExtraDeck(player);
        if (fusionTargets.Count == 0)
            return;

        FusionMonsterCard fusionCard;
        if (fusionTargets.Count == 1)
        {
            fusionCard = fusionTargets[0];
        }
        else
        {
            var fusionPrefs = new CardSelectorPrefs(PickFusionPrompt, 1, 1) { Cancelable = true };
            IEnumerable<CardModel> fusionPick;
            try
            {
                fusionPick = await CardSelectCmd.FromSimpleGrid(choiceContext, fusionTargets, player, fusionPrefs);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            FusionMonsterCard? picked = fusionPick.OfType<FusionMonsterCard>().FirstOrDefault();
            if (picked == null || !fusionTargets.Any(f => ReferenceEquals(f, picked)))
                return;
            fusionCard = picked;
        }

        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (extra == null || gy == null || !extra.Cards.Contains(fusionCard))
            return;

        if (!player.Creature.HasPower<RaDoomedPower>())
            await PowerCmd.Apply<RaDoomedPower>(player.Creature, 1m, player.Creature, source);

        decimal doomGain = source.DynamicVars["Mgc"].BaseValue;
        if (doomGain > 0m)
            await PowerCmd.Apply<DoomPower>(player.Creature, doomGain, player.Creature, source);

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await CardPileCmd.Add(new[] { fusionCard }, gy, CardPilePosition.Top, fusionCard, false);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 15m;
    }

    private static List<FusionMonsterCard> BuildFusionCardsInExtraDeck(Player player)
    {
        var list = new List<FusionMonsterCard>();
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        if (extra == null)
            return list;

        foreach (CardModel c in extra.Cards)
        {
            if (c is FusionMonsterCard fm)
                list.Add(fm);
        }

        return list;
    }
}
