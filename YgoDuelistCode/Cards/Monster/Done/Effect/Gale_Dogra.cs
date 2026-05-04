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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gale_Dogra : EffectMonsterCard, IMonsterActivatedEffect
{
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

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fusion | YgoCardPackTags.Insect | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Gale_Dogra) };

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
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GALE_DOGRA.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoPlayerRunPiles.IsYgoRunPlayer(Owner)
        && YgoFusionExtraDeckSelection.BuildFusionCardsInExtraDeck(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        List<FusionMonsterCard> fusionTargets = YgoFusionExtraDeckSelection.BuildFusionCardsInExtraDeck(player);
        if (fusionTargets.Count == 0)
            return;

        FusionMonsterCard? fusionCard = await YgoFusionExtraDeckSelection.TryChooseFusionFromExtraDeckAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickFusionPrompt, 1, 1) { Cancelable = true });
        if (fusionCard == null || !fusionTargets.Any(f => ReferenceEquals(f, fusionCard)))
            return;

        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
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

}
