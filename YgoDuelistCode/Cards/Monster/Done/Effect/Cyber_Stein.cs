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

public sealed class Cyber_Stein : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString PickFusionPrompt =
        new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    public Cyber_Stein()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 7,
            baseDef: 5,
            baseMgc: 45,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fusion | YgoCardPackTags.Machine | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[] { typeof(Cyber_Stein) };

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
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-CYBER_STEIN.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoPlayerRunPiles.IsYgoRunPlayer(Owner)
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && YgoFusionExtraDeckSelection.BuildFusionCardsInExtraDeck(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        List<FusionMonsterCard> fusionTargets = YgoFusionExtraDeckSelection.BuildFusionCardsInExtraDeck(player);
        if (fusionTargets.Count == 0 || !DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        FusionMonsterCard? fusionCard = await YgoFusionExtraDeckSelection.TryChooseFusionFromExtraDeckAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickFusionPrompt, 1, 1) { Cancelable = true });
        if (fusionCard == null || !fusionTargets.Any(f => ReferenceEquals(f, fusionCard)))
            return;

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
        DynamicVars["Mgc"].BaseValue = 30m;
    }

}
