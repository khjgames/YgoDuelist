using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Jigen_Bakudan : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipActivationPrompt =
        new("cards", "YGODUELIST-JIGEN_BAKUDAN.flip_preview.activation");
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");

    public override bool UseAlternateUpgradedDescription => true;

    public Jigen_Bakudan()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 2,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Jigen_Bakudan),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = IsUpgraded
                ? new("cards", "YGODUELIST-JIGEN_BAKUDAN.flip_effect.description_upgraded")
                : new("cards", "YGODUELIST-JIGEN_BAKUDAN.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Jigen_Bakudan || Owner == null || Owner.Creature?.CombatState == null)
            return;

        Player player = Owner;

        var activationPrefs = new CardSelectorPrefs(FlipActivationPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new[] { self }, player, activationPrefs);

        await PowerCmd.Apply<JigenBakudanPower>(Owner.Creature, 1m, Owner.Creature, self);
    }
}
