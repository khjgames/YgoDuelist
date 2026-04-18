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

public sealed class Magical_Scientist : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString PickFusionPrompt =
        new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    public Magical_Scientist()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 3,
            baseDef: 3,
            baseMgc: 15,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Fusion | YgoCardPackTags.Spellcaster | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[] { typeof(Magical_Scientist) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<DoomPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-MAGICAL_SCIENTIST.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && PlayerRunExtraDeck.IsYgoDuelistPlayer(Owner)
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && BuildFusionCardsInExtraDeck(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        List<FusionMonsterCard> fusionTargets = BuildFusionCardsInExtraDeck(player);
        if (fusionTargets.Count == 0 || !DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
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

        if (!player.Creature.HasPower<RaDoomedPower>())
            await PowerCmd.Apply<RaDoomedPower>(player.Creature, 1m, player.Creature, source);

        decimal doomGain = source.DynamicVars["Mgc"].BaseValue;
        if (doomGain > 0m)
            await PowerCmd.Apply<DoomPower>(player.Creature, doomGain, player.Creature, source);

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, fusionCard, choiceContext))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 10m;
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
