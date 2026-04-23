using System;
using System.Collections.Generic;
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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Woodland_Sprite : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString EquipPrompt = new("cards", "YGODUELIST-WOODLAND_SPRITE.equip_select");

    public Woodland_Sprite()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 4,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Woodland_Sprite) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-WOODLAND_SPRITE.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoEquipSpellRegistry.GetEquipsForMonster(this).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Woodland_Sprite sprite || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        IReadOnlyList<BaseEquipSpellCard> equips = BuildEquipCandidates(sprite);
        if (equips.Count == 0)
            return;

        BaseEquipSpellCard? equip = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(EquipPrompt, 1, 1) { Cancelable = true },
            () => BuildEquipCandidates(sprite));
        if (equip == null)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        YgoEquipSpellRegistry.Detach(equip);
        await CardPileCmd.Add(new[] { equip }, gy, CardPilePosition.Top, equip, false);

        int spellCost = Math.Max(0, equip.EnergyCost.Canonical);
        int energyGain = Math.Min(spellCost, 1);
        if (energyGain > 0)
            await PlayerCmd.GainEnergy(energyGain, player);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(sprite, player);
        if (cardPlay.Target == null || !cardPlay.Target.IsAlive || pet == null)
            return;

        int blight = (int)sprite.DynamicVars["Mgc"].BaseValue;
        if (blight > 0)
            await PowerCmd.Apply<BlightPower>(cardPlay.Target, blight, pet, sprite);

        await CardPileCmd.Draw(choiceContext, 1, player);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 7m;
    }

    private static List<BaseEquipSpellCard> BuildEquipCandidates(Woodland_Sprite sprite) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoEquipSpellRegistry.GetEquipsForMonster(sprite).Cast<CardModel>())
        .OfType<BaseEquipSpellCard>()
        .ToList();
}
