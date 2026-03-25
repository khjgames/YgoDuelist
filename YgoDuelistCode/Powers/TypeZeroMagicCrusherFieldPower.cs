using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Type_Zero_Magic_Crusher"/> is face-up in the Spell/Trap zone.</summary>
public sealed class TypeZeroMagicCrusherFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-TYPE_ZERO_MAGIC_CRUSHER_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-TYPE_ZERO_MAGIC_CRUSHER_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        List<Type_Zero_Magic_Crusher> crushers = zone?.Cards.OfType<Type_Zero_Magic_Crusher>().ToList() ?? [];

        if (crushers.Count == 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        Creature self = Owner;
        var combat = self.CombatState;
        if (combat == null)
            return;

        foreach (Type_Zero_Magic_Crusher trapCard in crushers)
        {
            CardPile? hand = PileType.Hand.GetPile(player);
            if (hand == null || !hand.Cards.Any(IsSpellInHand))
                continue;

            var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var pick = await CardSelectCmd.FromHand(choiceContext, player, prefs, IsSpellInHand, this);
            CardModel? spell = pick.FirstOrDefault();
            if (spell == null)
                continue;

            await CardCmd.Discard(choiceContext, spell);

            foreach (Creature enemy in combat.HittableEnemies.Where(e => e.IsAlive))
                await CreatureCmd.Damage(choiceContext, enemy, 5m, ValueProp.Unpowered, self, trapCard);
        }
    }

    private static bool IsSpellInHand(CardModel c) =>
        c is IYgoCard y && y.YgoCardType == YgoCardType.Spell;
}
