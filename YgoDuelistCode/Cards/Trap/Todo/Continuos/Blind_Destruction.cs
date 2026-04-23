using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Blind_Destruction : BaseContinuousTrapCard, IYgoOwnerTurnStartSpellTrapZoneEffect
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 12m) };

    public Blind_Destruction()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Chance | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Blind_Destruction),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return YgoDeterministicRngResultDisplay.Rolled6SampleHoverTip();
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(8m);
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    public bool IsOwnerTurnStartSpellTrapZoneEffectActive() => !FaceDown;

    public async Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null || !IsOwnerTurnStartSpellTrapZoneEffectActive())
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(player, "BLIND_DESTRUCTION"))
            return;

        CombatState cs = player.Creature.CombatState;
        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(player, this);
        int roll = YgoDeterministicRng.RollDie(cs, 6, "BLIND_DESTRUCTION-D6", mix);
        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, player, roll);

        var prompt = new LocString("cards", "YGODUELIST-BLIND_DESTRUCTION.die_result.selection");
        prompt.Add("Roll", (decimal)roll);
        var prefs = new CardSelectorPrefs(prompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new[] { resultCard }, player, prefs);

        decimal sixCase = DynamicVars["Mgc"].BaseValue;
        decimal dmg = roll == 6 ? sixCase : roll;
        foreach (Creature e in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await CreatureCmd.Damage(choiceContext, e, dmg, ValueProp.Unpowered, player.Creature, this);

        if (roll == 6 || player.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet.IsAlive)
                await CreatureCmd.Damage(choiceContext, pet, dmg, ValueProp.Unpowered, player.Creature, this);
        }
    }
}
