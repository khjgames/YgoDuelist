using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Jigen_Bakudan : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipActivationPrompt =
        new("cards", "YGODUELIST-JIGEN_BAKUDAN.flip_preview.activation");

    public override bool UseAlternateUpgradedDescription => true;

    public Jigen_Bakudan()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 2,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
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
        await CardSelectCmd.FromSimpleGrid(choiceContext, new[] { self }, player, activationPrefs);

        await PowerCmd.Apply<JigenBakudanPower>(Owner.Creature, 1m, Owner.Creature, self);
    }
}
